namespace Pinventory.Pins.Domain.Pins;

public sealed record Location
{
    public double Latitude { get; private set; }
    public double Longitude { get; private set;  }

    public Location(double latitude, double longitude)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90, nameof(latitude));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90, nameof(latitude));
        ArgumentOutOfRangeException.ThrowIfLessThan(longitude, -180, nameof(longitude));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(longitude, 180, nameof(longitude));

        Latitude = latitude;
        Longitude = longitude;
    }
}