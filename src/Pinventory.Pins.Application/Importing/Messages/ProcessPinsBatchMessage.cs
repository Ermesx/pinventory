using Nager.Country;

namespace Pinventory.Pins.Application.Importing.Messages;

public record ProcessPinsBatchMessage(string UserId, string ArchiveJobId, IEnumerable<StarredPlace> StarredPlaces) : ICorrelatedMessage
{
    public static ProcessPinsBatchMessage Create(ICorrelatedMessage message, IEnumerable<StarredPlace> starredPlaces) =>
        new(message.UserId, message.ArchiveJobId, starredPlaces);
}

public record StarredPlace(
    string? Name,
    string GoogleMapsUrl,
    string? Address,
    Alpha2Code? CountryCode,
    double? Latitude,
    double? Longitude,
    DateTimeOffset AddedDate,
    string? Comment);