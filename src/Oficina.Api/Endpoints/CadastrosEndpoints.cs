using Oficina.Api.Configuracao;
using Oficina.Application.Clientes;
using Oficina.Application.Veiculos;

namespace Oficina.Api.Endpoints;

public static class CadastrosEndpoints
{
    public static void MapClientes(this IEndpointRouteBuilder api)
    {
        var grupo = api.MapGroup("/clientes").WithTags("Clientes")
            .RequireAuthorization(Politicas.Atendimento);

        grupo.MapGet("/", (ClienteService s, int? pagina, int? tamanhoPagina, CancellationToken ct) =>
            s.ListarAsync(pagina, tamanhoPagina, ct));

        grupo.MapGet("/{id:guid}", (Guid id, ClienteService s, CancellationToken ct) => s.ObterAsync(id, ct));

        grupo.MapGet("/documento/{documento}", (string documento, ClienteService s, CancellationToken ct) =>
            s.ObterPorDocumentoAsync(documento, ct));

        grupo.MapPost("/", async (CriarClienteRequest request, ClienteService s, CancellationToken ct) =>
        {
            var cliente = await s.CriarAsync(request, ct);
            return TypedResults.Created($"/api/v1/clientes/{cliente.Id}", cliente);
        });

        grupo.MapPut("/{id:guid}", (Guid id, AtualizarClienteRequest request, ClienteService s, CancellationToken ct) =>
            s.AtualizarAsync(id, request, ct));

        grupo.MapDelete("/{id:guid}", async (Guid id, ClienteService s, CancellationToken ct) =>
        {
            await s.RemoverAsync(id, ct);
            return TypedResults.NoContent();
        });
    }

    public static void MapVeiculos(this IEndpointRouteBuilder api)
    {
        var grupo = api.MapGroup("/veiculos").WithTags("Veículos")
            .RequireAuthorization(Politicas.Atendimento);

        grupo.MapGet("/", (VeiculoService s, Guid? clienteId, int? pagina, int? tamanhoPagina, CancellationToken ct) =>
            s.ListarAsync(clienteId, pagina, tamanhoPagina, ct));

        grupo.MapGet("/{id:guid}", (Guid id, VeiculoService s, CancellationToken ct) => s.ObterAsync(id, ct));

        grupo.MapPost("/", async (CriarVeiculoRequest request, VeiculoService s, CancellationToken ct) =>
        {
            var veiculo = await s.CriarAsync(request, ct);
            return TypedResults.Created($"/api/v1/veiculos/{veiculo.Id}", veiculo);
        });

        grupo.MapPut("/{id:guid}", (Guid id, AtualizarVeiculoRequest request, VeiculoService s, CancellationToken ct) =>
            s.AtualizarAsync(id, request, ct));

        grupo.MapDelete("/{id:guid}", async (Guid id, VeiculoService s, CancellationToken ct) =>
        {
            await s.RemoverAsync(id, ct);
            return TypedResults.NoContent();
        });
    }
}
