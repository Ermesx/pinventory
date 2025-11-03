using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Domain.Importing;

namespace Pinventory.Pins.Infrastructure.Services;

public sealed class ImportConcurrencyPolicy(PinsDbContext dbContext) : IImportConcurrencyPolicy
{
    public async Task<bool> CanStartImportAsync(string userId, CancellationToken cancellationToken = default) =>
        !await dbContext.Imports
            .AnyAsync(import => import.UserId == userId && import.State == ImportState.InProgress, cancellationToken);
}