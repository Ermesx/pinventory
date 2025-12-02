namespace Pinventory.Pins.Domain.Importing;

public interface IStarredPlaceValidator
{
    Task<StarredPlaceState> ValidateAsync(Import import, StarredPlace place, CancellationToken cancellationToken = default);
}