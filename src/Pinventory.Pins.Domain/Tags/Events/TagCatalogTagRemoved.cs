using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Tags.Events;

public record TagCatalogTagRemoved(Guid AggregateId, string Tag) : DomainEvent(AggregateId);