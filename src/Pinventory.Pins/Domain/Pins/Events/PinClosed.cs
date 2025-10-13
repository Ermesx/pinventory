using Pinventory.Pins.Abstractions;

namespace Pinventory.Pins.Domain.Pins.Events;

public record PinClosed(Guid AggregateId, PinStatus Status, PinStatus PreviousStatus) : DomainEvent(AggregateId);