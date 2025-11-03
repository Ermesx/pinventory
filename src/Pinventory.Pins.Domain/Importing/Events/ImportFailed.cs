using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public record ImportFailed(Guid AggregateId, string Error) : DomainEvent(AggregateId);