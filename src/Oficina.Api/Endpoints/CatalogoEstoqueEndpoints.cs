using Oficina.Application.Catalogo;
using Oficina.Application.Estoque;

namespace Oficina.Api.Endpoints;

public static class CatalogoEstoqueEndpoints
{
    public static void MapServicos(this IEndpointRouteBuilder api)
    {
        var grupo = api.MapGroup("/servicos").WithTags("Serviços");

        grupo.MapGet("/", (ServicoService s, int? pagina, int? tamanhoPagina, CancellationToken ct) =>
            s.ListarAsync(pagina, tamanhoPagina, ct));

        grupo.MapGet("/{id:guid}", (Guid id, ServicoService s, CancellationToken ct) => s.ObterAsync(id, ct));

        grupo.MapPost("/", async (ServicoRequest request, ServicoService s, CancellationToken ct) =>
        {
            var servico = await s.CriarAsync(request, ct);
            return TypedResults.Created($"/api/v1/servicos/{servico.Id}", servico);
        });

        grupo.MapPut("/{id:guid}", (Guid id, ServicoRequest request, ServicoService s, CancellationToken ct) =>
            s.AtualizarAsync(id, request, ct));

        grupo.MapDelete("/{id:guid}", async (Guid id, ServicoService s, CancellationToken ct) =>
        {
            await s.RemoverAsync(id, ct);
            return TypedResults.NoContent();
        });
    }

    public static void MapPecas(this IEndpointRouteBuilder api)
    {
        var grupo = api.MapGroup("/pecas").WithTags("Peças e insumos");

        grupo.MapGet("/", (PecaService s, int? pagina, int? tamanhoPagina, CancellationToken ct) =>
            s.ListarAsync(pagina, tamanhoPagina, ct));

        grupo.MapGet("/{id:guid}", (Guid id, PecaService s, CancellationToken ct) => s.ObterAsync(id, ct));

        grupo.MapPost("/", async (CriarPecaRequest request, PecaService s, CancellationToken ct) =>
        {
            var peca = await s.CriarAsync(request, ct);
            return TypedResults.Created($"/api/v1/pecas/{peca.Id}", peca);
        });

        grupo.MapPut("/{id:guid}", (Guid id, AtualizarPecaRequest request, PecaService s, CancellationToken ct) =>
            s.AtualizarAsync(id, request, ct));

        grupo.MapDelete("/{id:guid}", async (Guid id, PecaService s, CancellationToken ct) =>
        {
            await s.RemoverAsync(id, ct);
            return TypedResults.NoContent();
        });

        grupo.MapPost("/{id:guid}/entradas", (Guid id, EntradaEstoqueRequest request, PecaService s,
            CancellationToken ct) => s.RegistrarEntradaAsync(id, request, ct));
    }
}
