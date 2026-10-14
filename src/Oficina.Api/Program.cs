using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Oficina.Api.Configuracao;
using Oficina.Api.Endpoints;
using Oficina.Application.Abstracoes;
using Oficina.Application.Catalogo;
using Oficina.Application.Clientes;
using Oficina.Application.Estoque;
using Oficina.Application.Identidade;
using Oficina.Application.OrdensDeServico;
using Oficina.Application.Veiculos;
using Oficina.Infrastructure;
using Oficina.Infrastructure.Persistencia;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSeguranca(builder.Configuration);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<VeiculoService>();
builder.Services.AddScoped<ServicoService>();
builder.Services.AddScoped<PecaService>();
builder.Services.AddScoped<OrdemDeServicoService>();

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratamentoDeErros>();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddHealthChecks().AddDbContextCheck<OficinaDbContext>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<ISenhaHasher>(), TimeProvider.System);
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Oficina API v1"));
app.MapHealthChecks("/health");

var api = app.MapGroup("/api/v1");
api.MapAutenticacao();
api.MapClientes();
api.MapVeiculos();
api.MapServicos();
api.MapPecas();
api.MapOrdensDeServico();
api.MapAreaDoCliente();

app.Run();

public partial class Program;
