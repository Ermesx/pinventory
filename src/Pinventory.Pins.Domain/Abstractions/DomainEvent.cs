namespace Pinventory.Pins.Domain.Abstractions;

public abstract record DomainEvent(Guid Id)
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}