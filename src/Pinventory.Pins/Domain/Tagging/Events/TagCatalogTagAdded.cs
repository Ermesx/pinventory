using Pinventory.Pins.Abstractions;

namespace Pinventory.Pins.Domain.Tagging.Events;

public record TagCatalogTagAdded(Guid AggregateId, string Tag) : DomainEvent(AggregateId);