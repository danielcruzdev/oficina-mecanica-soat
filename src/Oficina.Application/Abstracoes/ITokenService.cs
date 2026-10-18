using Oficina.Domain.Identidade;

namespace Oficina.Application.Abstracoes;

public sealed record TokenGerado(string AccessToken, DateTimeOffset ExpiraEm);

public interface ITokenService
{
    TokenGerado Gerar(Usuario usuario);
}
