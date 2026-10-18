using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Oficina.Infrastructure.Persistencia;

/// <summary>Usado só pelo "dotnet ef" para gerar migrations (não abre conexão).</summary>
internal sealed class OficinaDbContextFactory : IDesignTimeDbContextFactory<OficinaDbContext>
{
    public OficinaDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<OficinaDbContext>()
            .UseNpgsql("Host=localhost;Database=oficina")
            .UseSnakeCaseNamingConvention()
            .Options);
}
