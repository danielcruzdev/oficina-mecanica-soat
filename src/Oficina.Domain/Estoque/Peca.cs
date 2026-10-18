using Oficina.Domain.Comum;

namespace Oficina.Domain.Estoque;

public enum TipoItem
{
    Peca,
    Insumo
}

public sealed class Peca : AggregateRoot
{
    private readonly List<MovimentacaoEstoque> _movimentacoes = [];

    public string Sku { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public TipoItem Tipo { get; private set; }
    public string Unidade { get; private set; } = null!;
    public Dinheiro Preco { get; private set; } = null!;
    public int Saldo { get; private set; }
    public int EstoqueMinimo { get; private set; }
    public bool Ativo { get; private set; }

    /// <summary>Token de concorrência otimista (coluna de sistema xmin do PostgreSQL).</summary>
    public uint Versao { get; private set; }

    public IReadOnlyCollection<MovimentacaoEstoque> Movimentacoes => _movimentacoes;

    private Peca() { }

    public Peca(string sku, string nome, TipoItem tipo, string unidade, Dinheiro preco, int estoqueMinimo,
        int saldoInicial, DateTimeOffset agora)
    {
        if (saldoInicial < 0)
            throw new DadoInvalidoException("Saldo inicial não pode ser negativo.");

        Sku = Guarda.Obrigatorio(sku, "SKU").ToUpperInvariant();
        Ativo = true;
        Atualizar(nome, tipo, unidade, preco, estoqueMinimo);
        Raise(new PecaCadastrada(Id, Sku));

        if (saldoInicial > 0)
            RegistrarEntrada(saldoInicial, agora);
    }

    public void Atualizar(string nome, TipoItem tipo, string unidade, Dinheiro preco, int estoqueMinimo)
    {
        if (estoqueMinimo < 0)
            throw new DadoInvalidoException("Estoque mínimo não pode ser negativo.");

        Nome = Guarda.Obrigatorio(nome, "Nome");
        Tipo = tipo;
        Unidade = Guarda.Obrigatorio(unidade, "Unidade");
        Preco = preco;
        EstoqueMinimo = estoqueMinimo;
    }

    public bool TemDisponivel(int quantidade) => Saldo >= quantidade;

    public void RegistrarEntrada(int quantidade, DateTimeOffset agora)
    {
        if (quantidade <= 0)
            throw new DadoInvalidoException("Quantidade deve ser maior que zero.");

        Saldo += quantidade;
        _movimentacoes.Add(new MovimentacaoEstoque(Id, TipoMovimentacao.Entrada, quantidade, null, agora));
        Raise(new EntradaDeEstoqueRegistrada(Id, quantidade));
    }

    public void BaixarPorOS(int quantidade, Guid ordemDeServicoId, DateTimeOffset agora)
    {
        if (!TemDisponivel(quantidade))
            throw new DomainException($"Estoque insuficiente para \"{Nome}\": saldo {Saldo}, necessário {quantidade}.");

        Saldo -= quantidade;
        _movimentacoes.Add(new MovimentacaoEstoque(Id, TipoMovimentacao.SaidaOS, quantidade, ordemDeServicoId, agora));
        Raise(new EstoqueBaixadoPorOS(Id, ordemDeServicoId, quantidade));

        if (Saldo <= EstoqueMinimo)
            Raise(new EstoqueAbaixoDoMinimo(Id, Saldo, EstoqueMinimo));
    }

    public void Inativar() => Ativo = false;
}
