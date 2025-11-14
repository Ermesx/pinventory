using JasperFx.Core;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Infrastructure.Services;

public sealed class TagVerifier(PinsDbContext dbContext, IMemoryCache cache) : ITagVerifier
{
    public async Task<bool> IsAllowedAsync(string? ownerId, string tag, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var normalizedTag = tag.Trim().ToLower();
        var tags = await GetTagsAsync(ownerId, cancellationToken);
        return tags != null && tags.Contains(normalizedTag);
    }

    private async Task<List<string>?> GetTagsAsync(string? ownerId, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(ownerId ?? "global", async entry =>
        {
            entry.SetAbsoluteExpiration(5.Seconds());
            return await dbContext.TagCatalogs
                .Include(x => x.Tags)
                .Where(x => x.OwnerId == ownerId)
                .SelectMany(x => x.Tags.Select(y => y.Value))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        });
    }
}