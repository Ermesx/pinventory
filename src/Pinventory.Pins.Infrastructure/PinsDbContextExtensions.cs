using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Domain.Importing;

namespace Pinventory.Pins.Infrastructure;

public static class PinsDbContextExtensions
{
    public static async Task<Import?> GetCurrentImportAsync(this PinsDbContext dbContext, string userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Imports
            .Include(x => x.StarredPlaces)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.State == ImportState.InProgress, cancellationToken);
}