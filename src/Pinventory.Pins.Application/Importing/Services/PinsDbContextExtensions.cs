using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure;

namespace Pinventory.Pins.Application.Importing.Services;

public static class PinsDbContextExtensions
{
    public static async Task<Import?> GetCurrentImport(this PinsDbContext dbContext, string userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Imports
            .Include(x => x.Batches)
            .ThenInclude(x => x.StarredPlaces)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.State == ImportState.InProgress, cancellationToken);

    public static async Task<Import?>
        GetCurrentImport(this PinsDbContext dbContext, Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Imports
            .Include(x => x.Batches)
            .ThenInclude(x => x.StarredPlaces)
            .SingleOrDefaultAsync(x => x.Id == id && x.State == ImportState.InProgress, cancellationToken);
}