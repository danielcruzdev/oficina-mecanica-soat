using Oficina.Domain.Comum;

namespace Oficina.Domain.OrdensDeServico;

public sealed record OrdemDeServicoAberta(Guid OrdemDeServicoId, string Numero) : IDomainEvent;

public sealed record DiagnosticoIniciado(Guid OrdemDeServicoId) : IDomainEvent;

public sealed record ServicoIncluidoNaOS(Guid OrdemDeServicoId, Guid ServicoId) : IDomainEvent;

public sealed record PecaIncluidaNaOS(Guid OrdemDeServicoId, Guid PecaId, int Quantidade) : IDomainEvent;

public sealed record OrcamentoGerado(Guid OrdemDeServicoId, int Versao, decimal Total) : IDomainEvent;

public sealed record OrcamentoEnviado(Guid OrdemDeServicoId, decimal Total) : IDomainEvent;

public sealed record OrcamentoAprovado(Guid OrdemDeServicoId, decimal Total) : IDomainEvent;

public sealed record OrcamentoRecusado(Guid OrdemDeServicoId, string? Motivo) : IDomainEvent;

public sealed record ExecucaoIniciada(Guid OrdemDeServicoId) : IDomainEvent;

public sealed record ExecucaoFinalizada(Guid OrdemDeServicoId, TimeSpan Duracao) : IDomainEvent;

public sealed record VeiculoEntregue(Guid OrdemDeServicoId) : IDomainEvent;

public sealed record StatusDaOSAlterado(Guid OrdemDeServicoId, StatusOS? De, StatusOS Para) : IDomainEvent;
