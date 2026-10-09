using NSubstitute;
using Oficina.Application.Abstracoes;
using Oficina.Application.Clientes;
using Oficina.Application.Comum;
using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;
using Oficina.Domain.Identidade;

namespace Oficina.UnitTests.Application;

public class ClienteServiceTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly ISenhaHasher _hasher = Substitute.For<ISenhaHasher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ClienteService _service;

    public ClienteServiceTests()
    {
        _hasher.Gerar(Arg.Any<string>()).Returns("hash");
        _service = new ClienteService(_clientes, _usuarios, _hasher, _uow);
    }

    private static Cliente NovoCliente() => new(Documento.Criar("52998224725"), "Maria", null, null);

    [Fact]
    public async Task Criar_com_senha_cria_acesso_do_cliente()
    {
        var resposta = await _service.CriarAsync(new CriarClienteRequest("529.982.247-25", "Maria", null, null, "Senha@123"));

        Assert.Equal("52998224725", resposta.Documento);
        Assert.Equal(TipoPessoa.Fisica, resposta.TipoPessoa);
        _clientes.Received(1).Adicionar(Arg.Any<Cliente>());
        _usuarios.Received(1).Adicionar(Arg.Is<Usuario>(u => u.Papel == Papel.Cliente && u.Login == "52998224725"));
        await _uow.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Criar_sem_senha_nao_cria_acesso()
    {
        await _service.CriarAsync(new CriarClienteRequest("52998224725", "Maria", null, null, null));

        _usuarios.DidNotReceive().Adicionar(Arg.Any<Usuario>());
    }

    [Fact]
    public async Task Criar_com_senha_curta_lanca_dado_invalido()
    {
        await Assert.ThrowsAsync<DadoInvalidoException>(() =>
            _service.CriarAsync(new CriarClienteRequest("52998224725", "Maria", null, null, "123")));
        await _uow.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Criar_com_documento_duplicado_lanca_conflito()
    {
        _clientes.ObterPorDocumentoAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>()).Returns(NovoCliente());

        await Assert.ThrowsAsync<ConflitoException>(() =>
            _service.CriarAsync(new CriarClienteRequest("52998224725", "Maria", null, null, null)));
    }

    [Fact]
    public async Task Obter_inexistente_lanca_nao_encontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.ObterAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.ObterPorDocumentoAsync("52998224725"));
    }

    [Fact]
    public async Task Obter_por_id_e_por_documento()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _clientes.ObterPorDocumentoAsync(cliente.Documento, Arg.Any<CancellationToken>()).Returns(cliente);

        Assert.Equal(cliente.Id, (await _service.ObterAsync(cliente.Id)).Id);
        Assert.Equal(cliente.Id, (await _service.ObterPorDocumentoAsync("529.982.247-25")).Id);
    }

    [Fact]
    public async Task Listar_normaliza_paginacao()
    {
        _clientes.ListarAsync(1, 100, Arg.Any<CancellationToken>()).Returns([NovoCliente()]);

        var lista = await _service.ListarAsync(0, 500);

        Assert.Single(lista);
    }

    [Fact]
    public async Task Atualizar_e_remover_logicamente()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var atualizado = await _service.AtualizarAsync(cliente.Id, new AtualizarClienteRequest("Maria Souza", "m@x.com", null));
        await _service.RemoverAsync(cliente.Id);

        Assert.Equal("Maria Souza", atualizado.Nome);
        Assert.False(cliente.Ativo);
        await _uow.Received(2).SalvarAsync(Arg.Any<CancellationToken>());
    }
}
