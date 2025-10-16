namespace Pinventory.Pins.Abstractions;

public abstract class Entity(Guid? id)
{
    public Guid Id { get; } = id ?? Guid.NewGuid();

    public override bool Equals(object? obj) => obj is Entity other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}