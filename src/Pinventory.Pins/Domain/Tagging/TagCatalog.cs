using FluentResults;

using Pinventory.Pins.Abstractions;
using Pinventory.Pins.Domain.Pins;

namespace Pinventory.Pins.Domain.Tagging;

public sealed class TagCatalog(
    Guid? ownerUserId = null,
    Guid? id = null) : AggregateRoot(id)
{
    private TagCatalog() : this(null) { }

    private readonly HashSet<Tag> _tags = [];

    public Guid? OwnerUserId { get; private set; } = ownerUserId;
    public IReadOnlyCollection<Tag> Tags => _tags;

    public Result<IEnumerable<Tag>> DefineTags(IEnumerable<string> tags)
    {
        var distinctTags = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        _tags.Clear();
        List<Result<Tag>> results = [];
        foreach (var result in distinctTags.Select(Tag.Create))
        {
            results.Add(result);
            if (result.IsSuccess)
            {
                _tags.Add(result.Value);
            }
        }

        if (_tags.Any())
        {
            Raise(new Events.TagCatalogTagsDefined(Id, distinctTags));
        }

        return results.Any(r => !r.IsSuccess) ? Result.Merge(results.ToArray()) : Result.Ok();
    }

    public Result<Tag> AddTag(string tag)
    {
        var result = Tag.Create(tag);
        if (result.IsSuccess)
        {
            if (!_tags.Add(result.Value))
            {
                return Result.Fail(Errors.Tag.TagAlreadyExists(tag));
            }

            Raise(new Events.TagCatalogTagAdded(Id, tag));
        }

        return result;
    }

    public Result<Tag> RemoveTag(string tag)
    {
        var result = Tag.Create(tag);
        if (result.IsSuccess && _tags.Remove(result.Value))
        {
                Raise(new Events.TagCatalogTagRemoved(Id, tag));
                return Result.Ok();
        }

        return result;a
    }
}