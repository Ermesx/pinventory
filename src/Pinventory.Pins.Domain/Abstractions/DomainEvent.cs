namespace Pinventory.Pins.Domain.Abstractions;

public abstract record DomainEvent(Guid AggregateId)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}