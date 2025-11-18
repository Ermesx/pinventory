using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Places.Events;

public record PinTagsAssigned(Guid Id, IEnumerable<string> Tags) : DomainEvent(Id);