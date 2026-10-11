using NSubstitute;
using Oficina.Application.Abstracoes;
using Oficina.Application.Comum;
using Oficina.Application.OrdensDeServico;
using Oficina.Domain.Catalogo;
using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;
using Oficina.Domain.Estoque;
using Oficina.Domain.OrdensDeServico;
using Oficina.Domain.Veiculos;

namespace Oficina.UnitTests.Application;

public class OrdemDeServicoServiceTests
{
    private static readonly Guid Usuario = Guid.NewGuid();

    private readonly IOrdemDeServicoRepository _ordens = Substitute.For<IOrdemDeServicoRepository>();
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IVeiculoRepository _veiculos = Substitute.For<IVeiculoRepository>();
    private readonly IServicoRepository _servicos = Substitute.For<IServicoRepository>();
    private readonly IPecaRepository _pecas = Substitute.For<IPecaRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly OrdemDeServicoService _service;

    private readonly Cliente _cliente = new(Documento.Criar("52998224725"), "Maria", null, null);
    private readonly Veiculo _veiculo;
    private readonly Servico _servico = new("Alinhamento", null, Dinheiro.Criar(120m), 45);
    private readonly Peca _peca = new("FLT-001", "Filtro", TipoItem.Peca, "un", Dinheiro.Criar(35m), 1, 5, DateTimeOffset.UtcNow);

    public OrdemDeServicoServiceTests()
    {
        _veiculo = new Veiculo(_cliente.Id, Placa.Criar("ABC1D23"), "VW", "Gol", 2020);
        _clientes.ObterPorDocumentoAsync(_cliente.Documento, Arg.Any<CancellationToken>()).Returns(_cliente);
        _clientes.ObterPorIdAsync(_cliente.Id, Arg.Any<CancellationToken>()).Returns(_cliente);
        _veiculos.ObterPorIdAsync(_veiculo.Id, Arg.Any<CancellationToken>()).Returns(_veiculo);
        _servicos.ObterPorIdAsync(_servico.Id, Arg.Any<CancellationToken>()).Returns(_servico);
        _pecas.ObterPorIdAsync(_peca.Id, Arg.Any<CancellationToken>()).Returns(_peca);
        _pecas.ObterPorIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([_peca]);
        _ordens.ProximoSequencialAsync(Arg.Any<CancellationToken>()).Returns(123L);
        _service = new OrdemDeServicoService(_ordens, _clientes, _veiculos, _servicos, _pecas, _uow, TimeProvider.System);
    }

    private OrdemDeServico NovaOs()
    {
        var os = OrdemDeServico.Abrir("OS-2026-000001", _cliente.Id, _veiculo.Id, "Barulho", Usuario, DateTimeOffset.UtcNow);
        _ordens.ObterPorIdAsync(os.Id, Arg.Any<CancellationToken>()).Returns(os);
        return os;
    }

    private OrdemDeServico OsAguardandoAprovacao(int quantidadePeca = 2)
    {
        var os = NovaOs();
        os.IniciarDiagnostico(Usuario, DateTimeOffset.UtcNow);
        os.AdicionarServico(_servico.Id, _servico.Nome, _servico.PrecoMaoDeObra);
        os.AdicionarPeca(_peca.Id, _peca.Nome, quantidadePeca, _peca.Preco);
        os.EnviarOrcamento(Usuario, DateTimeOffset.UtcNow);
        return os;
    }

    [Fact]
    public async Task Abrir_com_veiculo_existente_gera_numero_sequencial()
    {
        _veiculos.ObterPorPlacaAsync(_veiculo.Placa, Arg.Any<CancellationToken>()).Returns(_veiculo);

        var os = await _service.AbrirAsync(
            new AbrirOrdemDeServicoRequest("529.982.247-25", new VeiculoDaOrdemRequest("ABC1D23", null, null, null), "Barulho"),
            Usuario);

        Assert.Matches(@"^OS-\d{4}-000123$", os.Numero);
        Assert.Equal(StatusOS.Recebida, os.Status);
        Assert.Equal(_veiculo.Id, os.Veiculo.Id);
        _veiculos.DidNotReceive().Adicionar(Arg.Any<Veiculo>());
        _ordens.Received(1).Adicionar(Arg.Any<OrdemDeServico>());
        await _uow.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Abrir_com_placa_nova_cadastra_o_veiculo_na_hora()
    {
        var os = await _service.AbrirAsync(
            new AbrirOrdemDeServicoRequest("52998224725", new VeiculoDaOrdemRequest("XYZ9A87", "Fiat", "Uno", 2015), "Revisão"),
            Usuario);

        Assert.Equal("XYZ9A87", os.Veiculo.Placa);
        _veiculos.Received(1).Adicionar(Arg.Is<Veiculo>(v => v.ClienteId == _cliente.Id));
    }

    [Fact]
    public async Task Abrir_com_placa_nova_sem_dados_do_veiculo_lanca_dado_invalido()
    {
        await Assert.ThrowsAsync<DadoInvalidoException>(() => _service.AbrirAsync(
            new AbrirOrdemDeServicoRequest("52998224725", new VeiculoDaOrdemRequest("XYZ9A87", null, null, null), "Revisão"),
            Usuario));
    }

    [Fact]
    public async Task Abrir_para_cliente_inexistente_lanca_nao_encontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.AbrirAsync(
            new AbrirOrdemDeServicoRequest("11.222.333/0001-81", new VeiculoDaOrdemRequest("ABC1D23", null, null, null), "X"),
            Usuario));
    }

    [Fact]
    public async Task Abrir_para_cliente_inativo_lanca_regra_de_negocio()
    {
        _cliente.Inativar();

        await Assert.ThrowsAsync<DomainException>(() => _service.AbrirAsync(
            new AbrirOrdemDeServicoRequest("52998224725", new VeiculoDaOrdemRequest("ABC1D23", null, null, null), "X"),
            Usuario));
    }

    [Fact]
    public async Task Abrir_com_veiculo_de_outro_cliente_lanca_regra_de_negocio()
    {
        var deOutro = new Veiculo(Guid.NewGuid(), Placa.Criar("ABC1D23"), "VW", "Gol", 2020);
        _veiculos.ObterPorPlacaAsync(deOutro.Placa, Arg.Any<CancellationToken>()).Returns(deOutro);

        await Assert.ThrowsAsync<DomainException>(() => _service.AbrirAsync(
            new AbrirOrdemDeServicoRequest("52998224725", new VeiculoDaOrdemRequest("ABC1D23", null, null, null), "X"),
            Usuario));
    }

    [Fact]
    public async Task Incluir_servico_e_peca_atualiza_orcamento()
    {
        var os = NovaOs();

        await _service.AdicionarServicoAsync(os.Id, new AdicionarServicoRequest(_servico.Id));
        var orcamento = await _service.AdicionarPecaAsync(os.Id, new AdicionarPecaRequest(_peca.Id, 2));

        Assert.Equal(190m, orcamento.Total);
        Assert.Equal(2, orcamento.Versao);
        Assert.Single(orcamento.Servicos);
        Assert.Single(orcamento.Pecas);
    }

    [Fact]
    public async Task Incluir_servico_inativo_ou_inexistente_e_rejeitado()
    {
        var os = NovaOs();
        _servico.Inativar();

        await Assert.ThrowsAsync<DomainException>(() => _service.AdicionarServicoAsync(os.Id, new AdicionarServicoRequest(_servico.Id)));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.AdicionarServicoAsync(os.Id, new AdicionarServicoRequest(Guid.NewGuid())));
    }

    [Fact]
    public async Task Incluir_peca_sem_saldo_considera_o_que_ja_foi_incluido()
    {
        var os = NovaOs();
        await _service.AdicionarPecaAsync(os.Id, new AdicionarPecaRequest(_peca.Id, 4));

        await Assert.ThrowsAsync<DomainException>(() => _service.AdicionarPecaAsync(os.Id, new AdicionarPecaRequest(_peca.Id, 2)));
        Assert.Equal(5, _peca.Saldo);
    }

    [Fact]
    public async Task Incluir_peca_inativa_ou_inexistente_e_rejeitado()
    {
        var os = NovaOs();
        _peca.Inativar();

        await Assert.ThrowsAsync<DomainException>(() => _service.AdicionarPecaAsync(os.Id, new AdicionarPecaRequest(_peca.Id, 1)));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.AdicionarPecaAsync(os.Id, new AdicionarPecaRequest(Guid.NewGuid(), 1)));
    }

    [Fact]
    public async Task Remover_itens_e_consultar_orcamento()
    {
        var os = NovaOs();
        var servico = (await _service.AdicionarServicoAsync(os.Id, new AdicionarServicoRequest(_servico.Id))).Servicos[0];
        var peca = (await _service.AdicionarPecaAsync(os.Id, new AdicionarPecaRequest(_peca.Id, 1))).Pecas[0];

        await _service.RemoverServicoAsync(os.Id, servico.Id);
        await _service.RemoverPecaAsync(os.Id, peca.Id);

        Assert.Equal(0m, (await _service.ObterOrcamentoAsync(os.Id)).Total);
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.RemoverServicoAsync(os.Id, Guid.NewGuid()));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.RemoverPecaAsync(os.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Diagnostico_e_envio_mudam_status()
    {
        var os = NovaOs();
        os.AdicionarServico(_servico.Id, _servico.Nome, _servico.PrecoMaoDeObra);

        Assert.Equal(StatusOS.EmDiagnostico, (await _service.IniciarDiagnosticoAsync(os.Id, Usuario)).Status);
        var enviada = await _service.EnviarOrcamentoAsync(os.Id, Usuario);

        Assert.Equal(StatusOS.AguardandoAprovacao, enviada.Status);
        Assert.Equal(3, enviada.Historico.Count);
    }

    [Fact]
    public async Task Aprovar_baixa_estoque_e_inicia_execucao_em_uma_transacao()
    {
        var os = OsAguardandoAprovacao(quantidadePeca: 2);

        var resposta = await _service.AprovarOrcamentoAsync(os.Id, _cliente.Id, Usuario);

        Assert.Equal(StatusOS.EmExecucao, resposta.Status);
        Assert.Equal(3, _peca.Saldo);
        Assert.Contains(_peca.Movimentacoes, m => m.Tipo == TipoMovimentacao.SaidaOS && m.OrdemDeServicoId == os.Id);
        await _uow.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Aprovar_sem_estoque_suficiente_nao_grava_nada()
    {
        var os = OsAguardandoAprovacao(quantidadePeca: 5);
        _peca.BaixarPorOS(1, Guid.NewGuid(), DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<DomainException>(() => _service.AprovarOrcamentoAsync(os.Id, _cliente.Id, Usuario));
        await _uow.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cliente_nao_aprova_nem_recusa_os_de_outro_cliente()
    {
        var os = OsAguardandoAprovacao();
        var outroCliente = Guid.NewGuid();

        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.AprovarOrcamentoAsync(os.Id, outroCliente, Usuario));
        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            _service.RecusarOrcamentoAsync(os.Id, outroCliente, Usuario, new RecusarOrcamentoRequest(null)));
        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
    }

    [Fact]
    public async Task Recusar_finaliza_sem_mexer_no_estoque()
    {
        var os = OsAguardandoAprovacao();

        var resposta = await _service.RecusarOrcamentoAsync(os.Id, _cliente.Id, Usuario, new RecusarOrcamentoRequest("Caro"));

        Assert.Equal(StatusOS.Finalizada, resposta.Status);
        Assert.Equal(ResultadoOS.OrcamentoRecusado, resposta.Resultado);
        Assert.Equal(5, _peca.Saldo);
    }

    [Fact]
    public async Task Finalizar_e_entregar()
    {
        var os = OsAguardandoAprovacao();
        await _service.AprovarOrcamentoAsync(os.Id, _cliente.Id, Usuario);

        var finalizada = await _service.FinalizarExecucaoAsync(os.Id, Usuario);
        var entregue = await _service.RegistrarEntregaAsync(os.Id, Usuario);

        Assert.Equal(StatusOS.Finalizada, finalizada.Status);
        Assert.NotNull(finalizada.TempoExecucaoMinutos);
        Assert.Equal(StatusOS.Entregue, entregue.Status);
        Assert.NotNull(entregue.EntregueEm);
    }

    [Fact]
    public async Task Os_inexistente_lanca_nao_encontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.ObterAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Obter_detalhe_traz_cliente_veiculo_e_historico()
    {
        var os = NovaOs();

        var detalhe = await _service.ObterAsync(os.Id);

        Assert.Equal(_cliente.Id, detalhe.Cliente.Id);
        Assert.Equal("ABC1D23", detalhe.Veiculo.Placa);
        Assert.Single(detalhe.Historico);
    }
}
