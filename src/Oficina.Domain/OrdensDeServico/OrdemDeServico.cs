using Oficina.Domain.Comum;

namespace Oficina.Domain.OrdensDeServico;

/// <summary>
/// Raiz do agregado. O status nunca é alterado diretamente: ele muda como consequência
/// das ações de negócio, sempre validado pela matriz de transições.
/// </summary>
public sealed class OrdemDeServico : AggregateRoot
{
    private static readonly Dictionary<StatusOS, StatusOS[]> Transicoes = new()
    {
        [StatusOS.Recebida] = [StatusOS.EmDiagnostico],
        [StatusOS.EmDiagnostico] = [StatusOS.AguardandoAprovacao],
        [StatusOS.AguardandoAprovacao] = [StatusOS.EmExecucao, StatusOS.Finalizada],
        [StatusOS.EmExecucao] = [StatusOS.Finalizada],
        [StatusOS.Finalizada] = [StatusOS.Entregue],
        [StatusOS.Entregue] = []
    };

    private readonly List<ItemServico> _servicos = [];
    private readonly List<ItemPeca> _pecas = [];
    private readonly List<HistoricoStatus> _historico = [];

    public string Numero { get; private set; } = null!;
    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }
    public string DescricaoProblema { get; private set; } = null!;
    public StatusOS Status { get; private set; }
    public DateTimeOffset AbertaEm { get; private set; }
    public DateTimeOffset? InicioExecucao { get; private set; }
    public DateTimeOffset? FimExecucao { get; private set; }
    public DateTimeOffset? EntregueEm { get; private set; }
    public ResultadoOS? Resultado { get; private set; }
    public Orcamento Orcamento { get; private set; } = new();

    public IReadOnlyCollection<ItemServico> Servicos => _servicos;
    public IReadOnlyCollection<ItemPeca> Pecas => _pecas;
    public IReadOnlyCollection<HistoricoStatus> Historico => _historico;

    public TimeSpan? TempoDeExecucao => FimExecucao - InicioExecucao;

    public bool ItensEditaveis => Status is StatusOS.Recebida or StatusOS.EmDiagnostico;

    private OrdemDeServico() { }

    public static OrdemDeServico Abrir(string numero, Guid clienteId, Guid veiculoId, string descricaoProblema,
        Guid usuarioId, DateTimeOffset agora)
    {
        var os = new OrdemDeServico
        {
            Numero = numero,
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            DescricaoProblema = Guarda.Obrigatorio(descricaoProblema, "Descrição do problema"),
            Status = StatusOS.Recebida,
            AbertaEm = agora
        };
        os._historico.Add(new HistoricoStatus(null, StatusOS.Recebida, usuarioId, agora));
        os.Raise(new OrdemDeServicoAberta(os.Id, numero));
        return os;
    }

    public void IniciarDiagnostico(Guid usuarioId, DateTimeOffset agora)
    {
        MudarStatus(StatusOS.EmDiagnostico, usuarioId, agora);
        Raise(new DiagnosticoIniciado(Id));
    }

    public ItemServico AdicionarServico(Guid servicoId, string nome, Dinheiro preco)
    {
        GarantirItensEditaveis();
        var item = new ItemServico(servicoId, nome, preco);
        _servicos.Add(item);
        Raise(new ServicoIncluidoNaOS(Id, servicoId));
        RecalcularOrcamento();
        return item;
    }

    public ItemPeca AdicionarPeca(Guid pecaId, string nome, int quantidade, Dinheiro precoUnitario)
    {
        GarantirItensEditaveis();
        if (quantidade <= 0)
            throw new DadoInvalidoException("Quantidade deve ser maior que zero.");

        var item = new ItemPeca(pecaId, nome, quantidade, precoUnitario);
        _pecas.Add(item);
        Raise(new PecaIncluidaNaOS(Id, pecaId, quantidade));
        RecalcularOrcamento();
        return item;
    }

    /// <returns>false se o item não existe na OS.</returns>
    public bool RemoverServico(Guid itemId)
    {
        GarantirItensEditaveis();
        if (_servicos.RemoveAll(i => i.Id == itemId) == 0)
            return false;

        RecalcularOrcamento();
        return true;
    }

    /// <returns>false se o item não existe na OS.</returns>
    public bool RemoverPeca(Guid itemId)
    {
        GarantirItensEditaveis();
        if (_pecas.RemoveAll(i => i.Id == itemId) == 0)
            return false;

        RecalcularOrcamento();
        return true;
    }

    /// <summary>Quantidade total por peça, usada na baixa de estoque.</summary>
    public IReadOnlyDictionary<Guid, int> QuantidadePorPeca() =>
        _pecas.GroupBy(p => p.PecaId).ToDictionary(g => g.Key, g => g.Sum(p => p.Quantidade));

    public void EnviarOrcamento(Guid usuarioId, DateTimeOffset agora)
    {
        GarantirTransicao(StatusOS.AguardandoAprovacao);
        if (_servicos.Count == 0 && _pecas.Count == 0)
            throw new DomainException("O orçamento precisa de pelo menos um serviço ou peça para ser enviado.");

        MudarStatus(StatusOS.AguardandoAprovacao, usuarioId, agora);
        Orcamento.Enviar(agora);
        Raise(new OrcamentoEnviado(Id, Orcamento.Total.Valor));
    }

    /// <summary>A baixa de estoque das peças é feita pelo caso de uso, na mesma transação.</summary>
    public void AprovarOrcamento(Guid usuarioId, DateTimeOffset agora)
    {
        MudarStatus(StatusOS.EmExecucao, usuarioId, agora);
        Orcamento.Aprovar(agora);
        InicioExecucao = agora;
        Raise(new OrcamentoAprovado(Id, Orcamento.Total.Valor));
        Raise(new ExecucaoIniciada(Id));
    }

    public void RecusarOrcamento(string? motivo, Guid usuarioId, DateTimeOffset agora)
    {
        if (Status != StatusOS.AguardandoAprovacao)
            throw new TransicaoDeStatusInvalidaException("Só é possível recusar um orçamento aguardando aprovação.");

        MudarStatus(StatusOS.Finalizada, usuarioId, agora);
        Orcamento.Recusar(Guarda.Opcional(motivo), agora);
        Resultado = ResultadoOS.OrcamentoRecusado;
        Raise(new OrcamentoRecusado(Id, Orcamento.MotivoRecusa));
    }

    public void FinalizarExecucao(Guid usuarioId, DateTimeOffset agora)
    {
        if (Status != StatusOS.EmExecucao)
            throw new TransicaoDeStatusInvalidaException("Só é possível finalizar uma OS em execução.");

        MudarStatus(StatusOS.Finalizada, usuarioId, agora);
        FimExecucao = agora;
        Resultado = ResultadoOS.Concluida;
        Raise(new ExecucaoFinalizada(Id, TempoDeExecucao!.Value));
    }

    public void RegistrarEntrega(Guid usuarioId, DateTimeOffset agora)
    {
        MudarStatus(StatusOS.Entregue, usuarioId, agora);
        EntregueEm = agora;
        Raise(new VeiculoEntregue(Id));
    }

    private void RecalcularOrcamento()
    {
        var totalServicos = _servicos.Aggregate(Dinheiro.Zero, (total, i) => total + i.Preco);
        var totalPecas = _pecas.Aggregate(Dinheiro.Zero, (total, i) => total + i.Subtotal);
        Orcamento.Recalcular(totalServicos, totalPecas);
        Raise(new OrcamentoGerado(Id, Orcamento.Versao, Orcamento.Total.Valor));
    }

    private void GarantirItensEditaveis()
    {
        if (!ItensEditaveis)
            throw new DomainException("Itens só podem ser alterados com a OS em Recebida ou EmDiagnostico.");
    }

    private void GarantirTransicao(StatusOS para)
    {
        if (!Transicoes[Status].Contains(para))
            throw new TransicaoDeStatusInvalidaException($"Transição de {Status} para {para} não é permitida.");
    }

    private void MudarStatus(StatusOS para, Guid usuarioId, DateTimeOffset agora)
    {
        GarantirTransicao(para);
        var de = Status;
        Status = para;
        _historico.Add(new HistoricoStatus(de, para, usuarioId, agora));
        Raise(new StatusDaOSAlterado(Id, de, para));
    }
}
