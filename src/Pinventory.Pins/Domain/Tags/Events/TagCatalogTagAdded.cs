using Pinventory.Pins.Abstractions;

namespace Pinventory.Pins.Domain.Tags.Events;

public record TagCatalogTagAdded(Guid AggregateId, string Tag) : DomainEvent(AggregateId);