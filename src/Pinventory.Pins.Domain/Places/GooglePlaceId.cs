namespace Pinventory.Pins.Domain.Places;

public sealed record GooglePlaceId
{
    private const string MapsUrlPrefix = "https://maps.google.com/?cid=";

    public static readonly GooglePlaceId Unknown = new("-");

    public GooglePlaceId(string Id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id, nameof(Id));
        this.Id = Id;
    }

    public string Id { get; }

    public string MapsUrl => $"{MapsUrlPrefix}{Id}";

    public static GooglePlaceId Parse(string mapsUrl) => new(mapsUrl[MapsUrlPrefix.Length..]);
}