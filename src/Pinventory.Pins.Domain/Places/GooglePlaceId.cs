using FluentResults;

namespace Pinventory.Pins.Domain.Places;

public sealed record GooglePlaceId
{
    private const string MapsUrlPrefix = "http://maps.google.com/?cid=";

    public static readonly GooglePlaceId Unknown = new("-");

    private GooglePlaceId(string id) => Id = id;

    public string Id { get; }

    public string MapsUrl => $"{MapsUrlPrefix}{Id}";

    public static Result<GooglePlaceId> Create(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Fail(Errors.GooglePlaceId.GooglePlaceIdCannotBeEmpty());
        }

        return new GooglePlaceId(id);
    }

    public static GooglePlaceId From(string id) => new(id);

    public static GooglePlaceId Parse(string mapsUrl) => new(mapsUrl[MapsUrlPrefix.Length..]);

    public static bool TryParse(string mapsUrl, out GooglePlaceId placeId)
    {
        if (mapsUrl.StartsWith(MapsUrlPrefix))
        {
            placeId = Parse(mapsUrl);
            return true;
        }

        placeId = Unknown;
        return false;
    }
}