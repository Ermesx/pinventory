using Pinventory.Pins.Abstractions;

namespace Pinventory.Pins.Domain.Pins.Events;

public record PinTagsAssigned(Guid AggregateId, IEnumerable<string> Tags) : DomainEvent(AggregateId);