namespace Oficina.Application.Abstracoes;

/// <summary>Grava todas as alterações do caso de uso em uma única transação.</summary>
public interface IUnitOfWork
{
    Task SalvarAsync(CancellationToken ct = default);
}
