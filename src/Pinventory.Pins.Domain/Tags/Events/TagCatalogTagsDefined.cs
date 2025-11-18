using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Tags.Events;

public record TagCatalogTagsDefined(Guid Id, IEnumerable<string> Tags) : DomainEvent(Id);