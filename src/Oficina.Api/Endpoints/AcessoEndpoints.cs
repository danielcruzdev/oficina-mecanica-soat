using System.Security.Claims;
using Oficina.Api.Configuracao;
using Oficina.Application.Identidade;
using Oficina.Application.OrdensDeServico;

namespace Oficina.Api.Endpoints;

public static class AcessoEndpoints
{
    public static void MapAutenticacao(this IEndpointRouteBuilder api)
    {
        var grupo = api.MapGroup("/auth").WithTags("Autenticação")
            .AllowAnonymous()
            .RequireRateLimiting(Politicas.LimiteLogin);

        grupo.MapPost("/login", (LoginRequest request, AuthService s, CancellationToken ct) =>
                s.LoginFuncionarioAsync(request, ct))
            .WithSummary("Login de funcionário (Administrador, Atendente, Mecânico)");

        grupo.MapPost("/clientes/login", (LoginClienteRequest request, AuthService s, CancellationToken ct) =>
                s.LoginClienteAsync(request, ct))
            .WithSummary("Login do cliente com CPF/CNPJ e senha");
    }

    /// <summary>API do cliente: sempre filtrada pelo cliente_id do token (OWASP API1 - BOLA).</summary>
    public static void MapAreaDoCliente(this IEndpointRouteBuilder api)
    {
        var grupo = api.MapGroup("/me/ordens-servico").WithTags("Área do cliente")
            .RequireAuthorization(Politicas.Cliente);

        grupo.MapGet("/", (OrdemDeServicoService s, ClaimsPrincipal u, CancellationToken ct) =>
            s.ListarDoClienteAsync(u.ClienteId(), ct));

        grupo.MapGet("/{id:guid}", (Guid id, OrdemDeServicoService s, ClaimsPrincipal u, CancellationToken ct) =>
            s.ObterDoClienteAsync(id, u.ClienteId(), ct));

        grupo.MapPost("/{id:guid}/orcamento/aprovacao", (Guid id, OrdemDeServicoService s, ClaimsPrincipal u,
            CancellationToken ct) => s.AprovarOrcamentoAsync(id, u.ClienteId(), u.UsuarioId(), ct));

        grupo.MapPost("/{id:guid}/orcamento/recusa", (Guid id, RecusarOrcamentoRequest? request,
            OrdemDeServicoService s, ClaimsPrincipal u, CancellationToken ct) =>
            s.RecusarOrcamentoAsync(id, u.ClienteId(), u.UsuarioId(), request ?? new RecusarOrcamentoRequest(null), ct));
    }
}
