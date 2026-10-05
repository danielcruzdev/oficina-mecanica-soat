using Oficina.Domain.Comum;
using Oficina.Domain.OrdensDeServico;

namespace Oficina.UnitTests.Domain;

public class OrdemDeServicoTests
{
    private static readonly Guid Usuario = Guid.NewGuid();
    private static readonly DateTimeOffset Agora = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private static OrdemDeServico NovaOs() =>
        OrdemDeServico.Abrir("OS-2026-000001", Guid.NewGuid(), Guid.NewGuid(), "Barulho no freio", Usuario, Agora);

    private static OrdemDeServico OsAguardandoAprovacao()
    {
        var os = NovaOs();
        os.IniciarDiagnostico(Usuario, Agora);
        os.AdicionarServico(Guid.NewGuid(), "Troca de óleo", Dinheiro.Criar(80m));
        os.EnviarOrcamento(Usuario, Agora);
        return os;
    }

    [Fact]
    public void Abrir_cria_os_recebida_com_historico_e_evento()
    {
        var os = NovaOs();

        Assert.Equal(StatusOS.Recebida, os.Status);
        Assert.Equal("OS-2026-000001", os.Numero);
        Assert.Equal(Agora, os.AbertaEm);
        var historico = Assert.Single(os.Historico);
        Assert.Null(historico.De);
        Assert.Equal(StatusOS.Recebida, historico.Para);
        Assert.Contains(os.Eventos, e => e is OrdemDeServicoAberta);
        Assert.Equal(0, os.Orcamento.Versao);
    }

    [Fact]
    public void Abrir_sem_descricao_lanca_dado_invalido()
    {
        Assert.Throws<DadoInvalidoException>(() =>
            OrdemDeServico.Abrir("OS-2026-000001", Guid.NewGuid(), Guid.NewGuid(), " ", Usuario, Agora));
    }

    [Fact]
    public void Orcamento_e_recalculado_a_cada_alteracao_de_item()
    {
        var os = NovaOs();
        os.AdicionarServico(Guid.NewGuid(), "Alinhamento", Dinheiro.Criar(120m));
        var peca = os.AdicionarPeca(Guid.NewGuid(), "Filtro de óleo", 2, Dinheiro.Criar(35.50m));

        Assert.Equal(120m, os.Orcamento.TotalServicos.Valor);
        Assert.Equal(71m, os.Orcamento.TotalPecas.Valor);
        Assert.Equal(191m, os.Orcamento.Total.Valor);
        Assert.Equal(2, os.Orcamento.Versao);

        Assert.True(os.RemoverPeca(peca.Id));

        Assert.Equal(120m, os.Orcamento.Total.Valor);
        Assert.Equal(3, os.Orcamento.Versao);
        Assert.Contains(os.Eventos, e => e is OrcamentoGerado { Versao: 3 });
    }

    [Fact]
    public void Remover_item_inexistente_retorna_false()
    {
        var os = NovaOs();

        Assert.False(os.RemoverServico(Guid.NewGuid()));
        Assert.False(os.RemoverPeca(Guid.NewGuid()));
    }

    [Fact]
    public void Remover_servico_recalcula_orcamento()
    {
        var os = NovaOs();
        var item = os.AdicionarServico(Guid.NewGuid(), "Alinhamento", Dinheiro.Criar(120m));

        Assert.True(os.RemoverServico(item.Id));
        Assert.Empty(os.Servicos);
        Assert.Equal(Dinheiro.Zero, os.Orcamento.Total);
    }

    [Fact]
    public void Peca_com_quantidade_zero_lanca_dado_invalido()
    {
        Assert.Throws<DadoInvalidoException>(() =>
            NovaOs().AdicionarPeca(Guid.NewGuid(), "Filtro", 0, Dinheiro.Criar(10m)));
    }

    [Fact]
    public void Quantidade_por_peca_soma_itens_da_mesma_peca()
    {
        var os = NovaOs();
        var pecaId = Guid.NewGuid();
        os.AdicionarPeca(pecaId, "Óleo", 2, Dinheiro.Criar(40m));
        os.AdicionarPeca(pecaId, "Óleo", 3, Dinheiro.Criar(40m));

        Assert.Equal(5, os.QuantidadePorPeca()[pecaId]);
    }

    [Fact]
    public void Enviar_orcamento_sem_itens_lanca_regra_de_negocio()
    {
        var os = NovaOs();
        os.IniciarDiagnostico(Usuario, Agora);

        var ex = Assert.Throws<DomainException>(() => os.EnviarOrcamento(Usuario, Agora));
        Assert.IsNotType<TransicaoDeStatusInvalidaException>(ex);
    }

    [Fact]
    public void Enviar_orcamento_muda_para_aguardando_aprovacao()
    {
        var os = OsAguardandoAprovacao();

        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
        Assert.Equal(SituacaoOrcamento.Enviado, os.Orcamento.Situacao);
        Assert.Equal(Agora, os.Orcamento.EnviadoEm);
        Assert.Contains(os.Eventos, e => e is OrcamentoEnviado { Total: 80m });
    }

    [Fact]
    public void Itens_nao_podem_ser_alterados_apos_envio()
    {
        var os = OsAguardandoAprovacao();

        Assert.False(os.ItensEditaveis);
        Assert.Throws<DomainException>(() => os.AdicionarServico(Guid.NewGuid(), "X", Dinheiro.Zero));
        Assert.Throws<DomainException>(() => os.AdicionarPeca(Guid.NewGuid(), "X", 1, Dinheiro.Zero));
        Assert.Throws<DomainException>(() => os.RemoverServico(Guid.NewGuid()));
        Assert.Throws<DomainException>(() => os.RemoverPeca(Guid.NewGuid()));
    }

    [Fact]
    public void Fluxo_completo_ate_entrega_registra_historico_e_tempo_de_execucao()
    {
        var os = OsAguardandoAprovacao();
        var inicio = Agora.AddHours(1);
        var fim = inicio.AddMinutes(90);

        os.AprovarOrcamento(Usuario, inicio);
        Assert.Equal(StatusOS.EmExecucao, os.Status);
        Assert.Equal(SituacaoOrcamento.Aprovado, os.Orcamento.Situacao);
        Assert.Equal(inicio, os.InicioExecucao);

        os.FinalizarExecucao(Usuario, fim);
        Assert.Equal(StatusOS.Finalizada, os.Status);
        Assert.Equal(ResultadoOS.Concluida, os.Resultado);
        Assert.Equal(TimeSpan.FromMinutes(90), os.TempoDeExecucao);

        os.RegistrarEntrega(Usuario, fim.AddHours(1));
        Assert.Equal(StatusOS.Entregue, os.Status);
        Assert.Equal(fim.AddHours(1), os.EntregueEm);

        Assert.Equal(
            [StatusOS.Recebida, StatusOS.EmDiagnostico, StatusOS.AguardandoAprovacao, StatusOS.EmExecucao,
                StatusOS.Finalizada, StatusOS.Entregue],
            os.Historico.Select(h => h.Para));
        Assert.Contains(os.Eventos, e => e is ExecucaoFinalizada);
        Assert.Contains(os.Eventos, e => e is VeiculoEntregue);
    }

    [Fact]
    public void Recusar_orcamento_finaliza_com_resultado_recusado()
    {
        var os = OsAguardandoAprovacao();

        os.RecusarOrcamento("Muito caro", Usuario, Agora);

        Assert.Equal(StatusOS.Finalizada, os.Status);
        Assert.Equal(ResultadoOS.OrcamentoRecusado, os.Resultado);
        Assert.Equal(SituacaoOrcamento.Recusado, os.Orcamento.Situacao);
        Assert.Equal("Muito caro", os.Orcamento.MotivoRecusa);
        Assert.Null(os.TempoDeExecucao);
    }

    [Fact]
    public void Os_recusada_pode_ser_entregue_mas_nao_finalizada_novamente()
    {
        var os = OsAguardandoAprovacao();
        os.RecusarOrcamento(null, Usuario, Agora);

        Assert.Throws<TransicaoDeStatusInvalidaException>(() => os.FinalizarExecucao(Usuario, Agora));
        os.RegistrarEntrega(Usuario, Agora);
        Assert.Equal(StatusOS.Entregue, os.Status);
    }

    public static TheoryData<Action<OrdemDeServico>> AcoesInvalidasEmRecebida => new()
    {
        os => os.EnviarOrcamento(Usuario, Agora),
        os => os.AprovarOrcamento(Usuario, Agora),
        os => os.RecusarOrcamento(null, Usuario, Agora),
        os => os.FinalizarExecucao(Usuario, Agora),
        os => os.RegistrarEntrega(Usuario, Agora)
    };

    [Theory]
    [MemberData(nameof(AcoesInvalidasEmRecebida))]
    public void Transicoes_invalidas_a_partir_de_recebida(Action<OrdemDeServico> acao)
    {
        var os = NovaOs();
        os.AdicionarServico(Guid.NewGuid(), "Troca de óleo", Dinheiro.Criar(80m));

        Assert.Throws<TransicaoDeStatusInvalidaException>(() => acao(os));
        Assert.Equal(StatusOS.Recebida, os.Status);
    }

    [Fact]
    public void Nao_pode_iniciar_diagnostico_duas_vezes()
    {
        var os = NovaOs();
        os.IniciarDiagnostico(Usuario, Agora);

        Assert.Throws<TransicaoDeStatusInvalidaException>(() => os.IniciarDiagnostico(Usuario, Agora));
    }

    [Fact]
    public void Nao_pode_aprovar_duas_vezes_nem_entregar_em_execucao()
    {
        var os = OsAguardandoAprovacao();
        os.AprovarOrcamento(Usuario, Agora);

        Assert.Throws<TransicaoDeStatusInvalidaException>(() => os.AprovarOrcamento(Usuario, Agora));
        Assert.Throws<TransicaoDeStatusInvalidaException>(() => os.RecusarOrcamento(null, Usuario, Agora));
        Assert.Throws<TransicaoDeStatusInvalidaException>(() => os.RegistrarEntrega(Usuario, Agora));
    }

    [Fact]
    public void Entregue_e_estado_final()
    {
        var os = OsAguardandoAprovacao();
        os.AprovarOrcamento(Usuario, Agora);
        os.FinalizarExecucao(Usuario, Agora);
        os.RegistrarEntrega(Usuario, Agora);

        Assert.Throws<TransicaoDeStatusInvalidaException>(() => os.RegistrarEntrega(Usuario, Agora));
        Assert.Throws<TransicaoDeStatusInvalidaException>(() => os.IniciarDiagnostico(Usuario, Agora));
    }

    [Fact]
    public void Toda_mudanca_de_status_emite_evento()
    {
        var os = OsAguardandoAprovacao();

        var mudancas = os.Eventos.OfType<StatusDaOSAlterado>().ToList();
        Assert.Equal(2, mudancas.Count);
        Assert.Equal(StatusOS.Recebida, mudancas[0].De);
        Assert.Equal(StatusOS.AguardandoAprovacao, mudancas[1].Para);

        os.LimparEventos();
        Assert.Empty(os.Eventos);
    }
}
