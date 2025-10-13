using Moq;

using Pinventory.Pins.Domain.Pins;
using Pinventory.Pins.Domain.Tagging;

namespace Pinventory.Pins.UnitTests.TestUtils;

public sealed class FakeTagVerifier(IEnumerable<string> allowed) : ITagVerifier
{
    private readonly HashSet<string> _allowed = [..allowed];

    public bool IsAllowed(string tag) => !string.IsNullOrWhiteSpace(tag) && _allowed.Contains(tag.Trim().ToLower());
}