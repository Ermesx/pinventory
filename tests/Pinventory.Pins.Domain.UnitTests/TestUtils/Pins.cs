using Nager.Country;

using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Domain.UnitTests.TestUtils;

public static class Pins
{
    public static Pin CreatePin(PinStatus initial = PinStatus.Open, string ownerId = "user123") =>
        new(ownerId, "Great Place", GooglePlaceId.From("g-123"), Address.From("123 Main St", Alpha2Code.PL), Location.From(0, 0),
            DateTimeOffset.UtcNow, initial);
}