using Pinventory.Pins.Abstractions;
using Pinventory.Pins.Domain.Pins;

namespace Pinventory.Pins.Domain.Import.Events;

public record PinImported(Guid AggregateId, GooglePlaceId GooglePlaceId) : DomainEvent(AggregateId);