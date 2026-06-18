using FluentResults;

namespace Pinventory.Pins.Domain.Places;

public sealed record Location
{
    public static readonly Location Default = new(0, 0);

    private Location(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }
    public double Longitude { get; }

    public static Result<Location> Create(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
        {
            return Result.Fail(Errors.Location.InvalidLatitude(latitude));
        }

        if (longitude is < -180 or > 180)
        {
            return Result.Fail(Errors.Location.InvalidLongitude(longitude));
        }

        return new Location(latitude, longitude);
    }

    public static Location From(double latitude, double longitude) => new(latitude, longitude);
}