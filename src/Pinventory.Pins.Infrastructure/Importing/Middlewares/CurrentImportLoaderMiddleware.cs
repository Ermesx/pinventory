using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Infrastructure.Importing.Middlewares;

public class CurrentImportLoaderMiddleware
{
    public async Task<Import?> LoadAsync(
        IUserMessage message,
        PinsDbContext dbContext,
        ILogger<CurrentImportLoaderMiddleware> logger,
        CancellationToken cancellationToken = default)
    {
        if (await GetCurrentImport(message.UserId) is not { } import)
        {
            logger.LogWarning("Running import not found for {UserId}", message.UserId);
            return null;
        }

        return import;

        async Task<Import?> GetCurrentImport(string userId) =>
            await dbContext.Imports
                .Include(x => x.StarredPlaces)
                .SingleOrDefaultAsync(x => x.UserId == userId && x.State == ImportState.InProgress, cancellationToken);
    }
}