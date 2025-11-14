using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing;

public sealed class Batch(IEnumerable<StarredPlace> starredPlaces, Guid? id = null) : Entity(id)
{
    private Batch() : this([]) { }
    public IReadOnlyCollection<StarredPlace> StarredPlaces { get; private set; } = starredPlaces.ToList();
}