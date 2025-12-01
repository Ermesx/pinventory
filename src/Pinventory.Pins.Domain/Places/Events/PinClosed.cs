using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Places.Events;

public record PinClosed(Guid Id, PinStatus Status, PinStatus PreviousStatus) : DomainEvent(Id);