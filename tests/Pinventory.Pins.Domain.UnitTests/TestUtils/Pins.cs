using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Domain.UnitTests.TestUtils;

public static class Pins
{
    public static Pin CreatePin(PinStatus initial = PinStatus.Open, string? ownerId = null) =>
        new(ownerId, new GooglePlaceId("g-123"), new Address("123 Main St"), new Location(0, 0), initial);
}