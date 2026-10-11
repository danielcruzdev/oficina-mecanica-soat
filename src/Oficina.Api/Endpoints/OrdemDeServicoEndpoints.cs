using System.Security.Claims;
using Oficina.Api.Configuracao;
using Oficina.Application.OrdensDeServico;

namespace Oficina.Api.Endpoints;

/// <summary>
/// Não existe endpoint "mudar status": o status muda como consequência de cada ação de negócio.
/// </summary>
public static class OrdemDeServicoEndpoints
{
    public static void MapOrdensDeServico(this IEndpointRouteBuilder api)
    {
        var grupo = api.MapGroup("/ordens-servico").WithTags("Ordens de serviço");

        grupo.MapPost("/", async (AbrirOrdemDeServicoRequest request, OrdemDeServicoService s, ClaimsPrincipal u,
            CancellationToken ct) =>
        {
            var os = await s.AbrirAsync(request, u.UsuarioId(), ct);
            return TypedResults.Created($"/api/v1/ordens-servico/{os.Id}", os);
        });

        grupo.MapGet("/{id:guid}", (Guid id, OrdemDeServicoService s, CancellationToken ct) => s.ObterAsync(id, ct));

        grupo.MapPost("/{id:guid}/diagnostico", (Guid id, OrdemDeServicoService s, ClaimsPrincipal u,
            CancellationToken ct) => s.IniciarDiagnosticoAsync(id, u.UsuarioId(), ct));

        grupo.MapPost("/{id:guid}/servicos", (Guid id, AdicionarServicoRequest request, OrdemDeServicoService s,
            CancellationToken ct) => s.AdicionarServicoAsync(id, request, ct));

        grupo.MapDelete("/{id:guid}/servicos/{itemId:guid}", (Guid id, Guid itemId, OrdemDeServicoService s,
            CancellationToken ct) => s.RemoverServicoAsync(id, itemId, ct));

        grupo.MapPost("/{id:guid}/pecas", (Guid id, AdicionarPecaRequest request, OrdemDeServicoService s,
            CancellationToken ct) => s.AdicionarPecaAsync(id, request, ct));

        grupo.MapDelete("/{id:guid}/pecas/{itemId:guid}", (Guid id, Guid itemId, OrdemDeServicoService s,
            CancellationToken ct) => s.RemoverPecaAsync(id, itemId, ct));

        grupo.MapGet("/{id:guid}/orcamento", (Guid id, OrdemDeServicoService s, CancellationToken ct) =>
            s.ObterOrcamentoAsync(id, ct));

        grupo.MapPost("/{id:guid}/orcamento/envio", (Guid id, OrdemDeServicoService s, ClaimsPrincipal u,
            CancellationToken ct) => s.EnviarOrcamentoAsync(id, u.UsuarioId(), ct));

        grupo.MapPost("/{id:guid}/execucao/finalizacao", (Guid id, OrdemDeServicoService s, ClaimsPrincipal u,
            CancellationToken ct) => s.FinalizarExecucaoAsync(id, u.UsuarioId(), ct));

        grupo.MapPost("/{id:guid}/entrega", (Guid id, OrdemDeServicoService s, ClaimsPrincipal u,
            CancellationToken ct) => s.RegistrarEntregaAsync(id, u.UsuarioId(), ct));
    }
}
