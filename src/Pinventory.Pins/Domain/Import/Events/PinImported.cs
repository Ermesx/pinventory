using Pinventory.Pins.Abstractions;
using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Domain.Import.Events;

public record PinImported(Guid AggregateId, GooglePlaceId GooglePlaceId) : DomainEvent(AggregateId);