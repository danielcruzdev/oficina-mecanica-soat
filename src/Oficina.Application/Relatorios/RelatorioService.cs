using Oficina.Domain.OrdensDeServico;

namespace Oficina.Application.Relatorios;

/// <param name="Quantidade">OS consideradas (execução iniciada e finalizada no período).</param>
public sealed record TempoMedioExecucaoResponse(int Quantidade, double? MediaMinutos, double? MinimoMinutos,
    double? MaximoMinutos);

public sealed class RelatorioService(IOrdemDeServicoRepository ordens)
{
    /// <summary>Tempo de execução = do início da execução (aprovação) até a finalização.</summary>
    public async Task<TempoMedioExecucaoResponse> TempoMedioExecucaoAsync(DateTimeOffset? de, DateTimeOffset? ate,
        Guid? servicoId, CancellationToken ct = default)
    {
        var minutos = (await ordens.ListarExecutadasAsync(de, ate, servicoId, ct))
            .Select(o => o.TempoDeExecucao!.Value.TotalMinutes)
            .ToList();

        return minutos.Count == 0
            ? new TempoMedioExecucaoResponse(0, null, null, null)
            : new TempoMedioExecucaoResponse(minutos.Count, Math.Round(minutos.Average(), 2), minutos.Min(),
                minutos.Max());
    }
}
