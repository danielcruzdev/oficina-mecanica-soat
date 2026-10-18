using System.Text.RegularExpressions;
using Oficina.Domain.Comum;

namespace Oficina.Domain.Veiculos;

/// <summary>Placa no formato antigo (AAA9999) ou Mercosul (AAA9A99).</summary>
public sealed partial record Placa
{
    public string Valor { get; }

    private Placa(string valor) => Valor = valor;

    public static Placa Criar(string? valor)
    {
        var placa = (valor ?? "").Replace("-", "").Trim().ToUpperInvariant();

        if (!Formato().IsMatch(placa))
            throw new DadoInvalidoException("Placa inválida.");

        return new Placa(placa);
    }

    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$")]
    private static partial Regex Formato();

    public override string ToString() => Valor;
}
