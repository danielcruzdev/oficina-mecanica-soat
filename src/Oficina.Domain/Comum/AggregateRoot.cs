namespace Oficina.Domain.Comum;

public interface IDomainEvent;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _eventos = [];

    public IReadOnlyCollection<IDomainEvent> Eventos => _eventos;

    protected void Raise(IDomainEvent evento) => _eventos.Add(evento);

    public void LimparEventos() => _eventos.Clear();
}
