using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Oficina.Application.Abstracoes;
using Oficina.Domain.Identidade;

namespace Oficina.Infrastructure.Seguranca;

public sealed class JwtOptions
{
    public const string Secao = "Jwt";

    /// <summary>Chave HS256 com no mínimo 32 caracteres (256 bits). Vem de variável de ambiente.</summary>
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "oficina-api";
    public string Audience { get; set; } = "oficina-clientes";
    public int ExpiracaoMinutos { get; set; } = 60;

    public SymmetricSecurityKey ChaveDeAssinatura() => new(Encoding.UTF8.GetBytes(Key));
}

internal sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider relogio) : ITokenService
{
    public TokenGerado Gerar(Usuario usuario)
    {
        var jwt = options.Value;
        var agora = relogio.GetUtcNow();
        var expiraEm = agora.AddMinutes(jwt.ExpiracaoMinutos);

        var claims = new List<Claim>
        {
            new(Claims.UsuarioId, usuario.Id.ToString()),
            new(Claims.Papel, usuario.Papel.ToString())
        };
        if (usuario.ClienteId is { } clienteId)
            claims.Add(new Claim(Claims.ClienteId, clienteId.ToString()));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            SigningCredentials = new SigningCredentials(jwt.ChaveDeAssinatura(), SecurityAlgorithms.HmacSha256)
        });

        return new TokenGerado(token, expiraEm);
    }
}
