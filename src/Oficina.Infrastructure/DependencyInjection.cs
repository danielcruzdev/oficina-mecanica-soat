using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Abstracoes;
using Oficina.Domain.Catalogo;
using Oficina.Domain.Clientes;
using Oficina.Domain.Estoque;
using Oficina.Domain.Identidade;
using Oficina.Domain.OrdensDeServico;
using Oficina.Domain.Veiculos;
using Oficina.Infrastructure.Persistencia;
using Oficina.Infrastructure.Persistencia.Repositorios;
using Oficina.Infrastructure.Seguranca;

namespace Oficina.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Oficina")
            ?? throw new InvalidOperationException("ConnectionStrings:Oficina não configurada.");

        services.AddDbContext<OficinaDbContext>(o => o.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OficinaDbContext>());

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IPecaRepository, PecaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IOrdemDeServicoRepository, OrdemDeServicoRepository>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ISenhaHasher, SenhaHasher>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Secao));
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
