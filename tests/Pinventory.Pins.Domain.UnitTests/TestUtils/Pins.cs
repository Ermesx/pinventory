using Nager.Country;

using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Domain.UnitTests.TestUtils;

public static class Pins
{
    public static Pin CreatePin(PinStatus initial = PinStatus.Open, string ownerId = "user123") =>
        new(ownerId, "Great Place", new GooglePlaceId("g-123"), new Address("123 Main St", Alpha2Code.PL), new Location(0, 0),
            DateTimeOffset.UtcNow, initial);
}