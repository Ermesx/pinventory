namespace Pinventory.Pins.Domain.Places;

public sealed record GooglePlaceId
{
    public static readonly GooglePlaceId Unknown = new("-");
    public string Id { get; }

    public GooglePlaceId(string Id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id, nameof(Id));
        this.Id = Id;
    }
}