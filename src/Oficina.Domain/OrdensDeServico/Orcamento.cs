using Oficina.Domain.Comum;

namespace Oficina.Domain.OrdensDeServico;

public enum SituacaoOrcamento
{
    Rascunho,
    Enviado,
    Aprovado,
    Recusado
}

/// <summary>Total de serviços + peças da OS. A versão sobe a cada alteração de item.</summary>
public sealed class Orcamento
{
    public int Versao { get; private set; }
    public Dinheiro TotalServicos { get; private set; } = Dinheiro.Zero;
    public Dinheiro TotalPecas { get; private set; } = Dinheiro.Zero;
    public Dinheiro Total { get; private set; } = Dinheiro.Zero;
    public SituacaoOrcamento Situacao { get; private set; } = SituacaoOrcamento.Rascunho;
    public DateTimeOffset? EnviadoEm { get; private set; }
    public DateTimeOffset? DecididoEm { get; private set; }
    public string? MotivoRecusa { get; private set; }

    internal void Recalcular(Dinheiro totalServicos, Dinheiro totalPecas)
    {
        TotalServicos = totalServicos;
        TotalPecas = totalPecas;
        Total = totalServicos + totalPecas;
        Versao++;
    }

    internal void Enviar(DateTimeOffset agora)
    {
        Situacao = SituacaoOrcamento.Enviado;
        EnviadoEm = agora;
    }

    internal void Aprovar(DateTimeOffset agora)
    {
        Situacao = SituacaoOrcamento.Aprovado;
        DecididoEm = agora;
    }

    internal void Recusar(string? motivo, DateTimeOffset agora)
    {
        Situacao = SituacaoOrcamento.Recusado;
        DecididoEm = agora;
        MotivoRecusa = motivo;
    }
}
