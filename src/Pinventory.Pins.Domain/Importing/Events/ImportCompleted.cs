using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public record ImportCompleted(Guid AggregateId) : DomainEvent(AggregateId);