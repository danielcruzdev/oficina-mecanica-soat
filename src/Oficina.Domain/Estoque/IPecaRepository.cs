namespace Oficina.Domain.Estoque;

public interface IPecaRepository
{
    Task<Peca?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Peca>> ObterPorIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<Peca>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken ct = default);
    void Adicionar(Peca peca);
}
