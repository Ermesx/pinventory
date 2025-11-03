using Pinventory.Pins.Domain.Abstractions;
using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Domain.Importing.Events;

public record PinImported(Guid AggregateId, GooglePlaceId GooglePlaceId) : DomainEvent(AggregateId);