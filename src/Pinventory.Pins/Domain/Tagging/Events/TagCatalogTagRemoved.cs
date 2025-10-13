using Pinventory.Pins.Abstractions;

namespace Pinventory.Pins.Domain.Tagging.Events;

public record TagCatalogTagRemoved(Guid AggregateId, string Tag) : DomainEvent(AggregateId);

