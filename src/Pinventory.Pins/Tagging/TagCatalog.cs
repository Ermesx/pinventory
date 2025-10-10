namespace Pinventory.Pins.Tagging;

public sealed class TagCatalog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? OwnerUserId { get; private set; } // null = global
    private readonly List<TagItem> _items = new();
    public IReadOnlyCollection<TagItem> Items => _items;

    private TagCatalog() { }
    public static TagCatalog Create(Guid? ownerUserId = null) => new() { OwnerUserId = ownerUserId };

    public void DefineTags(IEnumerable<string> tags)
    {
        if (tags is null) throw new ArgumentNullException(nameof(tags));
        var distinct = tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase);
        _items.Clear();
        foreach (var t in distinct) _items.Add(new TagItem(Id, t));
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentException("Tag is required", nameof(tag));
        var normalized = tag.Trim();
        if (_items.Any(x => string.Equals(x.Tag, normalized, StringComparison.OrdinalIgnoreCase))) return; // idempotent
        _items.Add(new TagItem(Id, normalized));
    }

    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        var normalized = tag.Trim();
        _items.RemoveAll(x => string.Equals(x.Tag, normalized, StringComparison.OrdinalIgnoreCase));
    }
}