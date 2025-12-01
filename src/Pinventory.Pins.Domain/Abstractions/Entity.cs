namespace Pinventory.Pins.Domain.Abstractions;

public abstract class Entity(Guid? id)
{
    // change this to: public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid Id { get; } = id ?? Guid.CreateVersion7();

    public override bool Equals(object? obj) => obj is Entity other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}