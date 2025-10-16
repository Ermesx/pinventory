namespace Pinventory.Pins.Abstractions;

public abstract class AggregateRoot(Guid? id) : Entity(id)
{
    private readonly List<DomainEvent> _domainEvents = [];

    public IReadOnlyCollection<object> DomainEvents => _domainEvents;

    public long Version { get; private set; }

    protected void Raise(DomainEvent @event) => _domainEvents.Add(@event);
}