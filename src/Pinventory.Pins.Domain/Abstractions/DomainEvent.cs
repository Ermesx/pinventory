namespace Pinventory.Pins.Domain.Abstractions;

public abstract record DomainEvent(Guid AggregateId)
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}