namespace Pinventory.Pins.Domain.Pins;

public sealed record GooglePlaceId
{
    public string Id { get; }

    public GooglePlaceId(string Id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace("Id is required", nameof(Id));
        this.Id = Id;
    }
}

