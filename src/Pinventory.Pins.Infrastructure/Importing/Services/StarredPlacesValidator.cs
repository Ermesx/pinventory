using JasperFx.Core;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Infrastructure.Importing.Services;

public class StarredPlacesValidator(PinsDbContext dbContext, IMemoryCache cache) : IStarredPlaceValidator
{
    private const string RemovedPlaceComment = "No location information is available for this saved place";

    public async Task<StarredPlaceState> ValidateAsync(Import import, StarredPlace place, CancellationToken cancellationToken = default)
    {
        if (!IsValidPlace(place) || !GooglePlaceId.TryParse(place.GoogleMapsUrl, out var placeId))
        {
            return StarredPlaceState.Invalid;
        }

        var existingPins = await GetUserPinsForImport(import, cancellationToken);
        if (existingPins is null)
        {
            return StarredPlaceState.New;
        }

        if (existingPins.Any(x => x.Name == place.Name && x.PlaceId != placeId))
        {
            return StarredPlaceState.Conflicted;
        }

        return existingPins.Any(x => x.PlaceId == placeId)
            ? StarredPlaceState.Exists
            : StarredPlaceState.New;

        static bool IsValidPlace(StarredPlace place) =>
            place is { Name: not null, Address: not null, Comment: not RemovedPlaceComment };
    }

    private async Task<List<Pin>?> GetUserPinsForImport(Import import, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(import.Id, async entry =>
        {
            entry.SetAbsoluteExpiration(10.Minutes());
            return await dbContext.Pins.Where(x => x.OwnerId == import.UserId).AsNoTracking().ToListAsync(cancellationToken);
        });
    }
}