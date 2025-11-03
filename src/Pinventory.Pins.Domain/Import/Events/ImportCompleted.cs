using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Import.Events;

public record ImportCompleted(Guid AggregateId) : DomainEvent(AggregateId);