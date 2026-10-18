using Microsoft.EntityFrameworkCore;
using Oficina.Domain.OrdensDeServico;
using Oficina.Infrastructure.Persistencia.Configuracoes;

namespace Oficina.Infrastructure.Persistencia.Repositorios;

internal sealed class OrdemDeServicoRepository(OficinaDbContext db) : IOrdemDeServicoRepository
{
    public Task<OrdemDeServico?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.OrdensDeServico
            .Include(o => o.Servicos)
            .Include(o => o.Pecas)
            .Include(o => o.Historico)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<long> ProximoSequencialAsync(CancellationToken ct) =>
        db.Database
            .SqlQueryRaw<long>($"SELECT nextval('{OrdemDeServicoConfiguracao.SequenciaNumero}') AS \"Value\"")
            .SingleAsync(ct);

    public async Task<IReadOnlyList<OrdemDeServico>> ListarAsync(FiltroOrdensDeServico filtro, CancellationToken ct)
    {
        var query = db.OrdensDeServico.AsNoTracking();

        query = filtro.Status is { } status
            ? query.Where(o => o.Status == status)
            : query.Where(o => o.Status != StatusOS.Finalizada && o.Status != StatusOS.Entregue);

        if (filtro.Documento is not null)
            query = query.Where(o => db.Clientes.Any(c => c.Id == o.ClienteId && c.Documento == filtro.Documento));
        if (filtro.Placa is not null)
            query = query.Where(o => db.Veiculos.Any(v => v.Id == o.VeiculoId && v.Placa == filtro.Placa));
        if (filtro.De is { } de)
            query = query.Where(o => o.AbertaEm >= de);
        if (filtro.Ate is { } ate)
            query = query.Where(o => o.AbertaEm <= ate);

        return await query
            .OrderBy(o => o.Status == StatusOS.EmExecucao ? 0
                : o.Status == StatusOS.AguardandoAprovacao ? 1
                : o.Status == StatusOS.EmDiagnostico ? 2
                : o.Status == StatusOS.Recebida ? 3
                : 4)
            .ThenBy(o => o.AbertaEm)
            .Pagina(filtro.Pagina, filtro.TamanhoPagina)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OrdemDeServico>> ListarPorClienteAsync(Guid clienteId, CancellationToken ct) =>
        await db.OrdensDeServico.AsNoTracking()
            .Where(o => o.ClienteId == clienteId)
            .OrderByDescending(o => o.AbertaEm)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<OrdemDeServico>> ListarExecutadasAsync(DateTimeOffset? de, DateTimeOffset? ate,
        Guid? servicoId, CancellationToken ct) =>
        await db.OrdensDeServico.AsNoTracking()
            .Where(o => o.InicioExecucao != null && o.FimExecucao != null)
            .Where(o => de == null || o.FimExecucao >= de)
            .Where(o => ate == null || o.FimExecucao <= ate)
            .Where(o => servicoId == null || o.Servicos.Any(s => s.ServicoId == servicoId))
            .ToListAsync(ct);

    public Task<bool> ExisteParaVeiculoAsync(Guid veiculoId, CancellationToken ct) =>
        db.OrdensDeServico.AnyAsync(o => o.VeiculoId == veiculoId, ct);

    public void Adicionar(OrdemDeServico ordemDeServico) => db.OrdensDeServico.Add(ordemDeServico);
}
