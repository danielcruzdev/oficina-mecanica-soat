namespace Oficina.Domain.Veiculos;

public interface IVeiculoRepository
{
    Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Veiculo?> ObterPorPlacaAsync(Placa placa, CancellationToken ct = default);
    Task<IReadOnlyList<Veiculo>> ListarAsync(Guid? clienteId, int pagina, int tamanhoPagina, CancellationToken ct = default);
    void Adicionar(Veiculo veiculo);
    void Remover(Veiculo veiculo);
}
