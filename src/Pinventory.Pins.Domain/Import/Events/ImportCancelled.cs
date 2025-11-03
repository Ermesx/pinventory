using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Import.Events;

public record ImportCancelled(Guid AggregateId) : DomainEvent(AggregateId);