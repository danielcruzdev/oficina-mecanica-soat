namespace Oficina.Application.Comum;

public static class Paginacao
{
    public const int TamanhoMaximo = 100;

    public static (int Pagina, int Tamanho) Normalizar(int? pagina, int? tamanhoPagina) =>
        (Math.Max(pagina ?? 1, 1), Math.Clamp(tamanhoPagina ?? 20, 1, TamanhoMaximo));
}
