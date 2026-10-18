using NSubstitute;
using Oficina.Application.Abstracoes;
using Oficina.Application.Comum;
using Oficina.Application.Veiculos;
using Oficina.Domain.Clientes;
using Oficina.Domain.OrdensDeServico;
using Oficina.Domain.Veiculos;

namespace Oficina.UnitTests.Application;

public class VeiculoServiceTests
{
    private readonly IVeiculoRepository _veiculos = Substitute.For<IVeiculoRepository>();
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IOrdemDeServicoRepository _ordens = Substitute.For<IOrdemDeServicoRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly VeiculoService _service;
    private readonly Cliente _cliente = new(Documento.Criar("52998224725"), "Maria", null, null);

    public VeiculoServiceTests()
    {
        _clientes.ObterPorIdAsync(_cliente.Id, Arg.Any<CancellationToken>()).Returns(_cliente);
        _service = new VeiculoService(_veiculos, _clientes, _ordens, _uow);
    }

    private Veiculo NovoVeiculo(string placa = "ABC1D23") => new(_cliente.Id, Placa.Criar(placa), "VW", "Gol", 2020);

    [Fact]
    public async Task Criar_veiculo_para_cliente_existente()
    {
        var resposta = await _service.CriarAsync(new CriarVeiculoRequest(_cliente.Id, "abc-1d23", "VW", "Gol", 2020));

        Assert.Equal("ABC1D23", resposta.Placa);
        _veiculos.Received(1).Adicionar(Arg.Any<Veiculo>());
        await _uow.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Criar_para_cliente_inexistente_lanca_nao_encontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            _service.CriarAsync(new CriarVeiculoRequest(Guid.NewGuid(), "ABC1D23", "VW", "Gol", 2020)));
    }

    [Fact]
    public async Task Criar_com_placa_duplicada_lanca_conflito()
    {
        _veiculos.ObterPorPlacaAsync(Arg.Any<Placa>(), Arg.Any<CancellationToken>()).Returns(NovoVeiculo());

        await Assert.ThrowsAsync<ConflitoException>(() =>
            _service.CriarAsync(new CriarVeiculoRequest(_cliente.Id, "ABC1D23", "VW", "Gol", 2020)));
    }

    [Fact]
    public async Task Atualizar_mantendo_a_propria_placa_e_permitido()
    {
        var veiculo = NovoVeiculo();
        _veiculos.ObterPorIdAsync(veiculo.Id, Arg.Any<CancellationToken>()).Returns(veiculo);
        _veiculos.ObterPorPlacaAsync(veiculo.Placa, Arg.Any<CancellationToken>()).Returns(veiculo);

        var resposta = await _service.AtualizarAsync(veiculo.Id, new AtualizarVeiculoRequest("ABC1D23", "VW", "Polo", 2021));

        Assert.Equal("Polo", resposta.Modelo);
    }

    [Fact]
    public async Task Atualizar_para_placa_de_outro_veiculo_lanca_conflito()
    {
        var veiculo = NovoVeiculo();
        _veiculos.ObterPorIdAsync(veiculo.Id, Arg.Any<CancellationToken>()).Returns(veiculo);
        _veiculos.ObterPorPlacaAsync(Placa.Criar("XYZ9876"), Arg.Any<CancellationToken>()).Returns(NovoVeiculo("XYZ9876"));

        await Assert.ThrowsAsync<ConflitoException>(() =>
            _service.AtualizarAsync(veiculo.Id, new AtualizarVeiculoRequest("XYZ9876", "VW", "Gol", 2020)));
    }

    [Fact]
    public async Task Remover_veiculo_sem_os()
    {
        var veiculo = NovoVeiculo();
        _veiculos.ObterPorIdAsync(veiculo.Id, Arg.Any<CancellationToken>()).Returns(veiculo);

        await _service.RemoverAsync(veiculo.Id);

        _veiculos.Received(1).Remover(veiculo);
    }

    [Fact]
    public async Task Remover_veiculo_com_os_lanca_conflito()
    {
        var veiculo = NovoVeiculo();
        _veiculos.ObterPorIdAsync(veiculo.Id, Arg.Any<CancellationToken>()).Returns(veiculo);
        _ordens.ExisteParaVeiculoAsync(veiculo.Id, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflitoException>(() => _service.RemoverAsync(veiculo.Id));
        _veiculos.DidNotReceive().Remover(Arg.Any<Veiculo>());
    }

    [Fact]
    public async Task Obter_e_listar()
    {
        var veiculo = NovoVeiculo();
        _veiculos.ObterPorIdAsync(veiculo.Id, Arg.Any<CancellationToken>()).Returns(veiculo);
        _veiculos.ListarAsync(_cliente.Id, 1, 20, Arg.Any<CancellationToken>()).Returns([veiculo]);

        Assert.Equal(veiculo.Id, (await _service.ObterAsync(veiculo.Id)).Id);
        Assert.Single(await _service.ListarAsync(_cliente.Id, null, null));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.ObterAsync(Guid.NewGuid()));
    }
}
