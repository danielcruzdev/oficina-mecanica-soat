using NSubstitute;
using Oficina.Application.Abstracoes;
using Oficina.Application.Comum;
using Oficina.Application.Identidade;
using Oficina.Domain.Clientes;
using Oficina.Domain.Identidade;

namespace Oficina.UnitTests.Application;

public class AuthServiceTests
{
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly ISenhaHasher _hasher = Substitute.For<ISenhaHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly AuthService _service;

    private readonly Usuario _admin = Usuario.Funcionario("admin@oficina.local", "hash", Papel.Administrador);
    private readonly Usuario _cliente = Usuario.DoCliente(new Cliente(Documento.Criar("52998224725"), "Maria", null, null), "hash");

    public AuthServiceTests()
    {
        _hasher.Verificar("hash", "certa").Returns(true);
        _usuarios.ObterPorLoginAsync("admin@oficina.local", Arg.Any<CancellationToken>()).Returns(_admin);
        _usuarios.ObterPorLoginAsync("52998224725", Arg.Any<CancellationToken>()).Returns(_cliente);
        _tokens.Gerar(Arg.Any<Usuario>()).Returns(new TokenGerado("jwt", DateTimeOffset.UtcNow.AddHours(1)));
        _service = new AuthService(_usuarios, _hasher, _tokens);
    }

    [Fact]
    public async Task Funcionario_com_senha_correta_recebe_token()
    {
        var token = await _service.LoginFuncionarioAsync(new LoginRequest("admin@oficina.local", "certa"));

        Assert.Equal("jwt", token.AccessToken);
        _tokens.Received(1).Gerar(_admin);
    }

    [Theory]
    [InlineData("admin@oficina.local", "errada")]
    [InlineData("naoexiste@oficina.local", "certa")]
    [InlineData("", "certa")]
    [InlineData("admin@oficina.local", "")]
    public async Task Funcionario_com_credenciais_invalidas_e_recusado(string login, string senha)
    {
        await Assert.ThrowsAsync<CredenciaisInvalidasException>(() =>
            _service.LoginFuncionarioAsync(new LoginRequest(login, senha)));
    }

    [Fact]
    public async Task Usuario_desativado_e_recusado()
    {
        _admin.Desativar();

        await Assert.ThrowsAsync<CredenciaisInvalidasException>(() =>
            _service.LoginFuncionarioAsync(new LoginRequest("admin@oficina.local", "certa")));
    }

    [Fact]
    public async Task Cliente_nao_entra_pelo_login_de_funcionario()
    {
        await Assert.ThrowsAsync<CredenciaisInvalidasException>(() =>
            _service.LoginFuncionarioAsync(new LoginRequest("52998224725", "certa")));
    }

    [Fact]
    public async Task Cliente_entra_com_documento_formatado()
    {
        var token = await _service.LoginClienteAsync(new LoginClienteRequest("529.982.247-25", "certa"));

        Assert.Equal("jwt", token.AccessToken);
        _tokens.Received(1).Gerar(_cliente);
    }

    [Fact]
    public async Task Cliente_com_documento_invalido_recebe_credenciais_invalidas()
    {
        await Assert.ThrowsAsync<CredenciaisInvalidasException>(() =>
            _service.LoginClienteAsync(new LoginClienteRequest("123", "certa")));
    }

    [Fact]
    public async Task Funcionario_nao_entra_pelo_login_de_cliente()
    {
        _usuarios.ObterPorLoginAsync("11222333000181", Arg.Any<CancellationToken>())
            .Returns(Usuario.Funcionario("11222333000181", "hash", Papel.Atendente));

        await Assert.ThrowsAsync<CredenciaisInvalidasException>(() =>
            _service.LoginClienteAsync(new LoginClienteRequest("11222333000181", "certa")));
    }
}
