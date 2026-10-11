using Oficina.Application.Clientes;
using Oficina.Application.Veiculos;
using Oficina.Domain.OrdensDeServico;

namespace Oficina.Application.OrdensDeServico;

/// <summary>Se a placa ainda não existir, marca, modelo e ano são usados para cadastrar o veículo na hora.</summary>
public sealed record VeiculoDaOrdemRequest(string Placa, string? Marca, string? Modelo, int? Ano);

public sealed record AbrirOrdemDeServicoRequest(string DocumentoCliente, VeiculoDaOrdemRequest Veiculo,
    string DescricaoProblema);

public sealed record AdicionarServicoRequest(Guid ServicoId);

public sealed record AdicionarPecaRequest(Guid PecaId, int Quantidade);

public sealed record RecusarOrcamentoRequest(string? Motivo);

public sealed record ItemServicoResponse(Guid Id, Guid ServicoId, string Nome, decimal Preco);

public sealed record ItemPecaResponse(Guid Id, Guid PecaId, string Nome, int Quantidade, decimal PrecoUnitario,
    decimal Subtotal);

public sealed record OrcamentoResponse(
    int Versao,
    decimal TotalServicos,
    decimal TotalPecas,
    decimal Total,
    SituacaoOrcamento Situacao,
    DateTimeOffset? EnviadoEm,
    DateTimeOffset? DecididoEm,
    string? MotivoRecusa,
    IReadOnlyList<ItemServicoResponse> Servicos,
    IReadOnlyList<ItemPecaResponse> Pecas)
{
    public static OrcamentoResponse De(OrdemDeServico os) => new(
        os.Orcamento.Versao,
        os.Orcamento.TotalServicos.Valor,
        os.Orcamento.TotalPecas.Valor,
        os.Orcamento.Total.Valor,
        os.Orcamento.Situacao,
        os.Orcamento.EnviadoEm,
        os.Orcamento.DecididoEm,
        os.Orcamento.MotivoRecusa,
        os.Servicos.Select(s => new ItemServicoResponse(s.Id, s.ServicoId, s.Nome, s.Preco.Valor)).ToList(),
        os.Pecas.Select(p => new ItemPecaResponse(p.Id, p.PecaId, p.Nome, p.Quantidade, p.PrecoUnitario.Valor,
            p.Subtotal.Valor)).ToList());
}

public sealed record HistoricoStatusResponse(StatusOS? De, StatusOS Para, DateTimeOffset OcorridoEm);

public sealed record OrdemDeServicoResumoResponse(Guid Id, string Numero, StatusOS Status, Guid ClienteId,
    Guid VeiculoId, DateTimeOffset AbertaEm, decimal Total)
{
    public static OrdemDeServicoResumoResponse De(OrdemDeServico os) =>
        new(os.Id, os.Numero, os.Status, os.ClienteId, os.VeiculoId, os.AbertaEm, os.Orcamento.Total.Valor);
}

public sealed record OrdemDeServicoResponse(
    Guid Id,
    string Numero,
    StatusOS Status,
    ResultadoOS? Resultado,
    string DescricaoProblema,
    ClienteResponse Cliente,
    VeiculoResponse Veiculo,
    OrcamentoResponse Orcamento,
    IReadOnlyList<HistoricoStatusResponse> Historico,
    DateTimeOffset AbertaEm,
    DateTimeOffset? InicioExecucao,
    DateTimeOffset? FimExecucao,
    DateTimeOffset? EntregueEm,
    double? TempoExecucaoMinutos);
