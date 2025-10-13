using Pinventory.Pins.Abstractions;

namespace Pinventory.Pins.Domain.Tagging.Events;

public record TagCatalogTagsDefined(Guid AggregateId, IEnumerable<string> Tags) : DomainEvent(AggregateId);

