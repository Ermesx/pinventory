using Pinventory.Pins.Abstractions;

namespace Pinventory.Pins.Domain.Tags.Events;

public record TagCatalogTagRemoved(Guid AggregateId, string Tag) : DomainEvent(AggregateId);