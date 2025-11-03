using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Domain.Import;

namespace Pinventory.Pins.Infrastructure.Services;

public sealed class ImportConcurrencyPolicy(PinsDbContext dbContext) : IImportConcurrencyPolicy
{
    public async Task<bool> CanStartImportAsync(string userId, CancellationToken cancellationToken = default) =>
        !await dbContext.ImportJobs
            .AnyAsync(job => job.UserId == userId && job.State == ImportJobState.InProgress, cancellationToken);
}