namespace Oficina.Domain.Comum;

internal static class Guarda
{
    public static string Obrigatorio(string? valor, string campo) =>
        string.IsNullOrWhiteSpace(valor)
            ? throw new DadoInvalidoException($"{campo} é obrigatório.")
            : valor.Trim();

    public static string? Opcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
