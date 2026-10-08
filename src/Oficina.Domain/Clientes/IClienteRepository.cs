namespace Oficina.Domain.Clientes;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Cliente?> ObterPorDocumentoAsync(Documento documento, CancellationToken ct = default);
    Task<IReadOnlyList<Cliente>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken ct = default);
    void Adicionar(Cliente cliente);
}
