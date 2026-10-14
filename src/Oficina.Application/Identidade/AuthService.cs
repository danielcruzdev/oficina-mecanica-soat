using Oficina.Application.Abstracoes;
using Oficina.Application.Comum;
using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;
using Oficina.Domain.Identidade;

namespace Oficina.Application.Identidade;

public sealed record LoginRequest(string Login, string Senha);

public sealed record LoginClienteRequest(string Documento, string Senha);

public sealed class AuthService(IUsuarioRepository usuarios, ISenhaHasher hasher, ITokenService tokens)
{
    public async Task<TokenGerado> LoginFuncionarioAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await Autenticar(request.Login, request.Senha, ct);
        return usuario.Papel == Papel.Cliente ? throw new CredenciaisInvalidasException() : tokens.Gerar(usuario);
    }

    public async Task<TokenGerado> LoginClienteAsync(LoginClienteRequest request, CancellationToken ct = default)
    {
        string login;
        try
        {
            login = Documento.Criar(request.Documento).Numero;
        }
        catch (DadoInvalidoException)
        {
            throw new CredenciaisInvalidasException();
        }

        var usuario = await Autenticar(login, request.Senha, ct);
        return usuario.Papel != Papel.Cliente ? throw new CredenciaisInvalidasException() : tokens.Gerar(usuario);
    }

    /// <summary>Mesma resposta para usuário inexistente e senha errada (não revela quais logins existem).</summary>
    private async Task<Usuario> Autenticar(string? login, string? senha, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(senha))
            throw new CredenciaisInvalidasException();

        var usuario = await usuarios.ObterPorLoginAsync(login, ct);
        if (usuario is null || !usuario.Ativo || !hasher.Verificar(usuario.SenhaHash, senha))
            throw new CredenciaisInvalidasException();

        return usuario;
    }
}
