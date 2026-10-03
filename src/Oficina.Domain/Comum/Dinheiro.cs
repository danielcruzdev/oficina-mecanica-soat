namespace Oficina.Domain.Comum;

/// <summary>Valor monetário em BRL, com 2 casas decimais e nunca negativo.</summary>
public sealed record Dinheiro
{
    public static readonly Dinheiro Zero = new(0m);

    public decimal Valor { get; }

    private Dinheiro(decimal valor) => Valor = valor;

    public static Dinheiro Criar(decimal valor)
    {
        if (valor < 0)
            throw new DadoInvalidoException("Valor monetário não pode ser negativo.");

        return new Dinheiro(Math.Round(valor, 2, MidpointRounding.AwayFromZero));
    }

    public static Dinheiro operator +(Dinheiro a, Dinheiro b) => Criar(a.Valor + b.Valor);

    public static Dinheiro operator *(Dinheiro a, int quantidade) => Criar(a.Valor * quantidade);

    public override string ToString() => Valor.ToString("F2");
}
