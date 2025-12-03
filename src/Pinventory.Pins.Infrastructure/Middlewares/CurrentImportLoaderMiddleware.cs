using Microsoft.Extensions.Logging;

using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Infrastructure.Middlewares;

public class CurrentImportLoaderMiddleware
{
    public async Task<Import?> LoadAsync(
        IUserMessage message,
        PinsDbContext dbContext,
        ILogger<CurrentImportLoaderMiddleware> logger,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.GetCurrentImportAsync(message.UserId, cancellationToken) is not { } import)
        {
            logger.LogError("Running import not found for {UserId}", message.UserId);
            return null;
        }

        return import;
    }
}