using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Oficina.Domain.Identidade;
using Oficina.Infrastructure.Seguranca;
using ClaimsOficina = Oficina.Infrastructure.Seguranca.Claims;

namespace Oficina.Api.Configuracao;

public static class Politicas
{
    public const string Administrador = nameof(Administrador);
    public const string Atendimento = nameof(Atendimento);
    public const string Oficina = nameof(Oficina);
    public const string Funcionario = nameof(Funcionario);
    public const string Cliente = nameof(Cliente);
    public const string LimiteLogin = nameof(LimiteLogin);
}

public static class Seguranca
{
    public static IServiceCollection AddSeguranca(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.Secao).Get<JwtOptions>() ?? new JwtOptions();
        if (jwt.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key não configurada ou com menos de 32 caracteres (256 bits).");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwt.ChaveDeAssinatura(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimsOficina.UsuarioId,
                    RoleClaimType = ClaimsOficina.Papel
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Politicas.Administrador, p => p.RequireRole(nameof(Papel.Administrador)))
            .AddPolicy(Politicas.Atendimento, p => p.RequireRole(nameof(Papel.Administrador), nameof(Papel.Atendente)))
            .AddPolicy(Politicas.Oficina, p => p.RequireRole(nameof(Papel.Administrador), nameof(Papel.Mecanico)))
            .AddPolicy(Politicas.Funcionario, p => p.RequireRole(
                nameof(Papel.Administrador), nameof(Papel.Atendente), nameof(Papel.Mecanico)))
            .AddPolicy(Politicas.Cliente, p => p.RequireRole(nameof(Papel.Cliente)).RequireClaim(ClaimsOficina.ClienteId));

        // Proteção contra força bruta no login: N tentativas por minuto por IP.
        var loginPorMinuto = configuration.GetValue("RateLimit:LoginPorMinuto", 5);
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(Politicas.LimiteLogin, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = loginPorMinuto, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }
}
