using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Tags.Events;

public record TagCatalogTagRemoved(Guid Id, string Tag) : DomainEvent(Id);