using Oficina.Domain.Comum;

namespace Oficina.Domain.OrdensDeServico;

/// <summary>Serviço do catálogo incluído na OS, com o preço congelado no momento da inclusão.</summary>
public sealed class ItemServico : Entity
{
    public Guid ServicoId { get; private set; }
    public string Nome { get; private set; } = null!;
    public Dinheiro Preco { get; private set; } = null!;

    private ItemServico() { }

    internal ItemServico(Guid servicoId, string nome, Dinheiro preco)
    {
        ServicoId = servicoId;
        Nome = nome;
        Preco = preco;
    }
}

/// <summary>Peça/insumo incluído na OS, com quantidade e preço unitário congelado.</summary>
public sealed class ItemPeca : Entity
{
    public Guid PecaId { get; private set; }
    public string Nome { get; private set; } = null!;
    public int Quantidade { get; private set; }
    public Dinheiro PrecoUnitario { get; private set; } = null!;

    public Dinheiro Subtotal => PrecoUnitario * Quantidade;

    private ItemPeca() { }

    internal ItemPeca(Guid pecaId, string nome, int quantidade, Dinheiro precoUnitario)
    {
        PecaId = pecaId;
        Nome = nome;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }
}

public sealed class HistoricoStatus : Entity
{
    public StatusOS? De { get; private set; }
    public StatusOS Para { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset OcorridoEm { get; private set; }

    private HistoricoStatus() { }

    internal HistoricoStatus(StatusOS? de, StatusOS para, Guid usuarioId, DateTimeOffset ocorridoEm)
    {
        De = de;
        Para = para;
        UsuarioId = usuarioId;
        OcorridoEm = ocorridoEm;
    }
}
