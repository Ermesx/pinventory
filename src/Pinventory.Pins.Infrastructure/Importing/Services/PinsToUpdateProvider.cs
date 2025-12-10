using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Infrastructure.Importing.Services;

public class PinsToUpdateProvider(PinsDbContext dbContext) : IPinsToUpdateProvider
{
    public async Task<IReadOnlyList<Pin>> GetPinsAsync(string userId, IEnumerable<GooglePlaceId> placeIdsToUpdate,
        CancellationToken cancellationToken = default) =>
        await dbContext.Pins
            .Where(x => x.OwnerId == userId && placeIdsToUpdate.Contains(x.PlaceId))
            .ToListAsync(cancellationToken);
}