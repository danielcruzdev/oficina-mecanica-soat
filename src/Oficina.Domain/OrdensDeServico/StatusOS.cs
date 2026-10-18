namespace Oficina.Domain.OrdensDeServico;

public enum StatusOS
{
    Recebida,
    EmDiagnostico,
    AguardandoAprovacao,
    EmExecucao,
    Finalizada,
    Entregue
}

public enum ResultadoOS
{
    Concluida,
    OrcamentoRecusado
}
