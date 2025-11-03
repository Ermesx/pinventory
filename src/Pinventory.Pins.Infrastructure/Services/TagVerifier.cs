using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Infrastructure.Services;

public sealed class TagVerifier(PinsDbContext dbContext) : ITagVerifier
{
    public bool IsAllowed(string? ownerId, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var normalizedTag = tag.Trim().ToLower();

        return dbContext.TagCatalogs
            .Where(catalog => catalog.OwnerId == ownerId)
            .SelectMany(catalog => catalog.Tags)
            .Any(t => t.Value == normalizedTag);
    }
}