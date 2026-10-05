using Oficina.Domain.Comum;

namespace Oficina.Domain.Estoque;

public sealed record PecaCadastrada(Guid PecaId, string Sku) : IDomainEvent;

public sealed record EntradaDeEstoqueRegistrada(Guid PecaId, int Quantidade) : IDomainEvent;

public sealed record EstoqueBaixadoPorOS(Guid PecaId, Guid OrdemDeServicoId, int Quantidade) : IDomainEvent;

public sealed record EstoqueAbaixoDoMinimo(Guid PecaId, int Saldo, int EstoqueMinimo) : IDomainEvent;
