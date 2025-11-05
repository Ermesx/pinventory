using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public record ImportCancelled(Guid AggregateId) : DomainEvent(AggregateId);