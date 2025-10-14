using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.UnitTests.TestUtils;

public static class Tagging
{
    public static IReadOnlyCollection<string> VerifiedTags = ["foo", "bar"];

    public static ITagVerifier CreateTagVerifier(IEnumerable<string>? tags = null) => new FakeTagVerifier(tags ?? VerifiedTags);
}