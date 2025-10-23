namespace Pinventory.Pins.Application.Tags.Commands;

public abstract record OwnerCommand(string? OwnerId)
{
    public bool IsGlobal => OwnerId is null;
}