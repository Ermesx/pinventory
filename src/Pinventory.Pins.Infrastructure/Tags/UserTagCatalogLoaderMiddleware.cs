using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Domain.Tags;
using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Infrastructure.Tags;

public class UserTagCatalogLoaderMiddleware
{
    public async Task<TagCatalog?> LoadAsync(IOwnerCommand command, PinsDbContext dbContext,
        CancellationToken cancellationToken = default) =>
        await dbContext.TagCatalogs
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.OwnerId == command.OwnerId, cancellationToken);
}