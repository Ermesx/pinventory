using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Tags.Events;

public record TagCatalogTagAdded(Guid AggregateId, string Tag) : DomainEvent(AggregateId);