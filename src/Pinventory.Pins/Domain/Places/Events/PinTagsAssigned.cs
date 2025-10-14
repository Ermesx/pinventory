using Pinventory.Pins.Abstractions;

namespace Pinventory.Pins.Domain.Places.Events;

public record PinTagsAssigned(Guid AggregateId, IEnumerable<string> Tags) : DomainEvent(AggregateId);