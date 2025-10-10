using Pinventory.Pins;

namespace Pinventory.Pins.Events;

public record PinImported(Guid PinId, GooglePlaceId GooglePlaceId);