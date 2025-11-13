using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure;

namespace Pinventory.Pins.Application.Importing;

public static class PinsDbContextExtensions
{
    public static async Task<Import?> GetCurrentImport(this PinsDbContext dbContext, string userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Imports
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.State == ImportState.InProgress, cancellationToken);

    public static async Task<Import?> GetCurrentImport(this PinsDbContext dbContext, ICorrelatedMessage message,
        CancellationToken cancellationToken = default) =>
        await dbContext.Imports
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                x => x.UserId == message.UserId && x.ArchiveJobId == message.ArchiveJobId && x.State == ImportState.InProgress,
                cancellationToken);
}