using Oficina.Domain.Clientes;
using Oficina.Domain.Veiculos;

namespace Oficina.Domain.OrdensDeServico;

/// <param name="Status">Sem status, a listagem traz só as OS em andamento (exclui Finalizada e Entregue).</param>
public sealed record FiltroOrdensDeServico(
    StatusOS? Status = null,
    Documento? Documento = null,
    Placa? Placa = null,
    DateTimeOffset? De = null,
    DateTimeOffset? Ate = null,
    int Pagina = 1,
    int TamanhoPagina = 20);

public interface IOrdemDeServicoRepository
{
    /// <summary>Carrega a OS com itens e histórico.</summary>
    Task<OrdemDeServico?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task<long> ProximoSequencialAsync(CancellationToken ct = default);

    /// <summary>Ordem de prioridade: EmExecucao, AguardandoAprovacao, EmDiagnostico, Recebida; depois as mais antigas.</summary>
    Task<IReadOnlyList<OrdemDeServico>> ListarAsync(FiltroOrdensDeServico filtro, CancellationToken ct = default);

    Task<IReadOnlyList<OrdemDeServico>> ListarPorClienteAsync(Guid clienteId, CancellationToken ct = default);

    /// <summary>OS com execução finalizada (início e fim preenchidos), filtradas pelo fim da execução e pelo serviço.</summary>
    Task<IReadOnlyList<OrdemDeServico>> ListarExecutadasAsync(DateTimeOffset? de, DateTimeOffset? ate, Guid? servicoId,
        CancellationToken ct = default);

    Task<bool> ExisteParaVeiculoAsync(Guid veiculoId, CancellationToken ct = default);

    void Adicionar(OrdemDeServico ordemDeServico);
}
