using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Oficina.IntegrationTests.Infra;

/// <summary>Sobe a API real contra um PostgreSQL em contêiner (migrations + seed rodam no start).</summary>
public sealed class OficinaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _banco = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async Task InitializeAsync() => await _banco.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _banco.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Oficina", _banco.GetConnectionString());
        builder.UseSetting("Jwt:Key", "chave-usada-somente-nos-testes-de-integracao-0123456789");
        builder.UseSetting("RateLimit:LoginPorMinuto", "10000");
    }
}

[CollectionDefinition(Nome)]
public sealed class ApiCollection : ICollectionFixture<OficinaApiFactory>
{
    public const string Nome = "api";
}
