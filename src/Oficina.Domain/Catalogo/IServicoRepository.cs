namespace Oficina.Domain.Catalogo;

public interface IServicoRepository
{
    Task<Servico?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Servico>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken ct = default);
    void Adicionar(Servico servico);
}
