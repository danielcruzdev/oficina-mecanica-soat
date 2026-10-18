using Oficina.Domain.Comum;

namespace Oficina.Domain.Estoque;

public enum TipoMovimentacao
{
    Entrada,
    SaidaOS
}

public sealed class MovimentacaoEstoque : Entity
{
    public Guid PecaId { get; private set; }
    public TipoMovimentacao Tipo { get; private set; }
    public int Quantidade { get; private set; }
    public Guid? OrdemDeServicoId { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    private MovimentacaoEstoque() { }

    internal MovimentacaoEstoque(Guid pecaId, TipoMovimentacao tipo, int quantidade, Guid? ordemDeServicoId,
        DateTimeOffset criadoEm)
    {
        PecaId = pecaId;
        Tipo = tipo;
        Quantidade = quantidade;
        OrdemDeServicoId = ordemDeServicoId;
        CriadoEm = criadoEm;
    }
}
