using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing;

public sealed class Batch(IEnumerable<StarredPlace> starredPlaces, Guid? id = null) : Entity(id)
{
    private Batch() : this([]) { }
    public IReadOnlyCollection<StarredPlace> StarredPlaces { get; private set; } = starredPlaces.ToList();

    public string BatchThumbprint => HashExtensions.GetThumbprint(hash =>
    {
        foreach (var place in StarredPlaces
                     .OrderBy(place => place.GoogleMapsUrl)
                     .ThenBy(place => place.AddedDate))
        {
            hash.AddString(place.GoogleMapsUrl);
            hash.AddString(place.AddedDate.ToString("O"));
        }
    });
}