using Pinventory.Pins.Domain.Pins;

namespace Pinventory.Pins.UnitTests.TestUtils;

public static class Pins
{
    public static Pin CreatePin(PinStatus initial = PinStatus.Open) => new(new GooglePlaceId("g-123"), new Address("123 Main St"), new Location(0, 0), initial);
}