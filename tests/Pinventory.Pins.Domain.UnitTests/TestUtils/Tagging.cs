using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Domain.UnitTests.TestUtils;

public static class Tagging
{
    public static IReadOnlyCollection<string> VerifiedTags => ["foo", "bar"];

    public static ITagVerifier CreateTagVerifier(IEnumerable<string>? tags = null) => new FakeTagVerifier(tags ?? VerifiedTags);
}