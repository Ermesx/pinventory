namespace Pinventory.Google;

public static class GoogleScopes
{
    public static readonly ICollection<string> DefaultScopes = ["openid", "email", "profile"];

    public const string DataPortabilityMapsStarredPlaces = "https://www.googleapis.com/auth/dataportability.maps.starred_places";

    public static class DataPortabilityResources
    {
        public const string MapsStarredPlaces = "maps.starred_places";
    }
}