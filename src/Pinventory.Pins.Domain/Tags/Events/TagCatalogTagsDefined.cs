using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Tags.Events;

public record TagCatalogTagsDefined(Guid AggregateId, IEnumerable<string> Tags) : DomainEvent(AggregateId);