using System.Security.Claims;
using ClaimsOficina = Oficina.Infrastructure.Seguranca.Claims;

namespace Oficina.Api.Configuracao;

public static class UsuarioAtual
{
    public static Guid UsuarioId(this ClaimsPrincipal usuario) =>
        Guid.TryParse(usuario.FindFirstValue(ClaimsOficina.UsuarioId), out var id) ? id : Guid.Empty;

    /// <summary>O cliente_id vem sempre do token, nunca da rota ou do corpo da requisição.</summary>
    public static Guid ClienteId(this ClaimsPrincipal usuario) =>
        Guid.Parse(usuario.FindFirstValue(ClaimsOficina.ClienteId)!);
}
