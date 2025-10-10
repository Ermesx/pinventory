namespace Pinventory.Pins.Tagging;

public sealed class TagItem
{
    public Guid CatalogId { get; private set; }

    public string Tag { get; private set; } = default!;

    private TagItem() { }

    public TagItem(Guid catalogId, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentException("Tag is required", nameof(tag));
        CatalogId = catalogId;
        Tag = tag.Trim();
    }
}