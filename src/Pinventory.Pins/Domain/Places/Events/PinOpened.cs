using Pinventory.Pins.Abstractions;

namespace Pinventory.Pins.Domain.Places.Events;

public record PinOpened(Guid AggregateId, PinStatus Status, PinStatus PreviousStatus): DomainEvent(AggregateId);