namespace Pinventory.Pins.Domain.Importing;

public interface IStaredPlaceValidator
{
    Task<StarredPlaceState> ValidateAsync(Import import, StarredPlace place, CancellationToken cancellationToken = default);
}