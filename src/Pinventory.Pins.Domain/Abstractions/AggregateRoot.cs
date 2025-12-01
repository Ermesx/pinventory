using System.ComponentModel.DataAnnotations;

namespace Pinventory.Pins.Domain.Abstractions;

public abstract class AggregateRoot(Guid? id) : Entity(id)
{
    private readonly List<DomainEvent> _domainEvents = [];

    public IReadOnlyCollection<object> DomainEvents => _domainEvents;

    [Timestamp]
    public uint Version { get; protected set; }

    protected void Raise(DomainEvent @event) => _domainEvents.Add(@event);
}