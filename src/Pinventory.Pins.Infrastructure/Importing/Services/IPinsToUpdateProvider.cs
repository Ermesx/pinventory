using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Infrastructure.Importing.Services;

public interface IPinsToUpdateProvider
{
    Task<IReadOnlyList<Pin>> GetPinsAsync(string userId, IEnumerable<GooglePlaceId> placeIds,
        CancellationToken cancellationToken = default);
}