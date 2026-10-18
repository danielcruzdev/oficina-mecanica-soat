using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Oficina.Application.Abstracoes;
using Oficina.Application.Comum;
using Oficina.Domain.Catalogo;
using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;
using Oficina.Domain.Estoque;
using Oficina.Domain.Identidade;
using Oficina.Domain.OrdensDeServico;
using Oficina.Domain.Veiculos;

namespace Oficina.Infrastructure.Persistencia;

/// <summary>Um DbContext por request = Unit of Work. Cada SalvarAsync é uma transação.</summary>
public sealed class OficinaDbContext(DbContextOptions<OficinaDbContext> options, ILogger<OficinaDbContext>? logger = null)
    : DbContext(options), IUnitOfWork
{
    private readonly ILogger _logger = logger ?? NullLogger<OficinaDbContext>.Instance;

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Peca> Pecas => Set<Peca>();
    public DbSet<OrdemDeServico> OrdensDeServico => Set<OrdemDeServico>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(Configuracoes.OrdemDeServicoConfiguracao.SequenciaNumero);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OficinaDbContext).Assembly);

        // Os Ids são gerados no domínio (Guid v7); assim o EF trata itens novos em coleções como inseridos.
        foreach (var entidade in modelBuilder.Model.GetEntityTypes().Where(e => typeof(Entity).IsAssignableFrom(e.ClrType)))
            entidade.FindProperty(nameof(Entity.Id))!.ValueGenerated = ValueGenerated.Never;
    }

    public async Task SalvarAsync(CancellationToken ct = default)
    {
        var agregados = ChangeTracker.Entries<AggregateRoot>().Select(e => e.Entity).ToList();
        var eventos = agregados.SelectMany(a => a.Eventos).ToList();

        try
        {
            await SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflitoException("O registro foi alterado por outra operação. Tente novamente.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflitoException("Já existe um registro com esses dados.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ConflitoException("O registro está em uso por outro cadastro.");
        }

        // Eventos de domínio são despachados in-process depois do commit (sem mensageria no MVP).
        foreach (var evento in eventos)
            _logger.LogInformation("Evento de domínio {Evento}: {@Dados}", evento.GetType().Name, evento);

        agregados.ForEach(a => a.LimparEventos());
    }
}
