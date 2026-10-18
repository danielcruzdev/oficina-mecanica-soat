using System.Security.Claims;
using Oficina.Api.Configuracao;
using Oficina.Application.OrdensDeServico;
using Oficina.Application.Relatorios;

namespace Oficina.Api.Endpoints;

/// <summary>
/// Não existe endpoint "mudar status": o status muda como consequência de cada ação de negócio.
/// </summary>
public static class OrdemDeServicoEndpoints
{
    public static void MapOrdensDeServico(this IEndpointRouteBuilder api)
    {
        var grupo = api.MapGroup("/ordens-servico").WithTags("Ordens de serviço")
            .RequireAuthorization(Politicas.Funcionario);

        grupo.MapPost("/", async (AbrirOrdemDeServicoRequest request, OrdemDeServicoService s, ClaimsPrincipal u,
            CancellationToken ct) =>
        {
            var os = await s.AbrirAsync(request, u.UsuarioId(), ct);
            return TypedResults.Created($"/api/v1/ordens-servico/{os.Id}", os);
        }).RequireAuthorization(Politicas.Atendimento);

        grupo.MapGet("/", ([AsParameters] ListarOrdensDeServicoRequest request, OrdemDeServicoService s,
                CancellationToken ct) => s.ListarAsync(request, ct))
            .WithSummary("Lista OS priorizadas: EmExecucao > AguardandoAprovacao > EmDiagnostico > Recebida, " +
                         "mais antigas primeiro. Finalizada/Entregue só com filtro de status.");

        grupo.MapGet("/{id:guid}", (Guid id, OrdemDeServicoService s, CancellationToken ct) => s.ObterAsync(id, ct));

        grupo.MapPost("/{id:guid}/diagnostico", (Guid id, OrdemDeServicoService s, ClaimsPrincipal u,
            CancellationToken ct) => s.IniciarDiagnosticoAsync(id, u.UsuarioId(), ct))
            .RequireAuthorization(Politicas.Oficina);

        grupo.MapPost("/{id:guid}/servicos", (Guid id, AdicionarServicoRequest request, OrdemDeServicoService s,
            CancellationToken ct) => s.AdicionarServicoAsync(id, request, ct))
            .RequireAuthorization(Politicas.Oficina);

        grupo.MapDelete("/{id:guid}/servicos/{itemId:guid}", (Guid id, Guid itemId, OrdemDeServicoService s,
            CancellationToken ct) => s.RemoverServicoAsync(id, itemId, ct))
            .RequireAuthorization(Politicas.Oficina);

        grupo.MapPost("/{id:guid}/pecas", (Guid id, AdicionarPecaRequest request, OrdemDeServicoService s,
            CancellationToken ct) => s.AdicionarPecaAsync(id, request, ct))
            .RequireAuthorization(Politicas.Oficina);

        grupo.MapDelete("/{id:guid}/pecas/{itemId:guid}", (Guid id, Guid itemId, OrdemDeServicoService s,
            CancellationToken ct) => s.RemoverPecaAsync(id, itemId, ct))
            .RequireAuthorization(Politicas.Oficina);

        grupo.MapGet("/{id:guid}/orcamento", (Guid id, OrdemDeServicoService s, CancellationToken ct) =>
            s.ObterOrcamentoAsync(id, ct));

        grupo.MapPost("/{id:guid}/orcamento/envio", (Guid id, OrdemDeServicoService s, ClaimsPrincipal u,
            CancellationToken ct) => s.EnviarOrcamentoAsync(id, u.UsuarioId(), ct))
            .RequireAuthorization(Politicas.Atendimento);

        grupo.MapPost("/{id:guid}/execucao/finalizacao", (Guid id, OrdemDeServicoService s, ClaimsPrincipal u,
            CancellationToken ct) => s.FinalizarExecucaoAsync(id, u.UsuarioId(), ct))
            .RequireAuthorization(Politicas.Oficina);

        grupo.MapPost("/{id:guid}/entrega", (Guid id, OrdemDeServicoService s, ClaimsPrincipal u,
            CancellationToken ct) => s.RegistrarEntregaAsync(id, u.UsuarioId(), ct))
            .RequireAuthorization(Politicas.Atendimento);
    }

    public static void MapRelatorios(this IEndpointRouteBuilder api)
    {
        api.MapGroup("/relatorios").WithTags("Relatórios")
            .RequireAuthorization(Politicas.Administrador)
            .MapGet("/tempo-medio-execucao", (DateTimeOffset? de, DateTimeOffset? ate, Guid? servicoId,
                RelatorioService s, CancellationToken ct) => s.TempoMedioExecucaoAsync(de, ate, servicoId, ct))
            .WithSummary("Tempo médio, mínimo e máximo de execução (em minutos) das OS finalizadas no período");
    }
}
