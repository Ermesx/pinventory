using Nager.Country;

using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing;

public sealed class StarredPlace(
    string? name,
    string googleMapsUrl,
    string? address,
    Alpha2Code? countryCode,
    double? latitude,
    double? longitude,
    DateTimeOffset addedDate,
    string? comment,
    Guid? id = null) : Entity(id)
{
    private StarredPlace() : this(string.Empty, string.Empty, string.Empty, Alpha2Code.PL, 0, 0, DateTimeOffset.UtcNow, string.Empty) { }
    public string? Name { get; } = name;
    public string GoogleMapsUrl { get; } = googleMapsUrl;
    public string? Address { get; } = address;
    public Alpha2Code? CountryCode { get; } = countryCode;
    public double? Latitude { get; } = latitude;
    public double? Longitude { get; } = longitude;
    public DateTimeOffset AddedDate { get; } = addedDate;
    public string? Comment { get; } = comment;
    public StarredPlaceState State { get; internal set; } = StarredPlaceState.New;
    public bool IsProcessed { get; internal set; }

    public string Thumbprint => HashExtensions.GetThumbprint(hash =>
    {
        hash.AddString(GoogleMapsUrl);
        hash.AddString(AddedDate.ToString("O"));
    });
}