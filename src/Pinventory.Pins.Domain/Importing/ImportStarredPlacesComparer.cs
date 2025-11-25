namespace Pinventory.Pins.Domain.Importing;

public class ImportStarredPlaceComparer : IEqualityComparer<StarredPlace>
{
    public bool Equals(StarredPlace? x, StarredPlace? y)
    {
        return x?.Thumbprint == y?.Thumbprint;
    }

    public int GetHashCode(StarredPlace obj)
    {
        return obj.Thumbprint.GetHashCode();
    }
}