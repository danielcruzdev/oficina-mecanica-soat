using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstracoes;
using Oficina.Infrastructure;
using Oficina.Infrastructure.Persistencia;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<OficinaDbContext>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<ISenhaHasher>(), TimeProvider.System);
}

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
