using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Domain.UnitTests.TestUtils;

public sealed class FakeTagVerifier(IEnumerable<string> allowed) : ITagVerifier
{
    private readonly HashSet<string> _allowed = [.. allowed];

    public bool IsAllowed(string? ownerId, string tag) => !string.IsNullOrWhiteSpace(tag) && _allowed.Contains(tag.Trim().ToLower());
}