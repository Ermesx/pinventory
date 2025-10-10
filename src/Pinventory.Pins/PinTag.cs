using System.ComponentModel.DataAnnotations;

namespace Pinventory.Pins;

public sealed class PinTag
{
    public Guid PinId { get; private set; }

    public string Tag { get; private set; } = default!;

    private PinTag() { }

    public PinTag(Guid pinId, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentException("Tag is required", nameof(tag));
        PinId = pinId;
        Tag = tag.Trim();
    }
}