namespace Pinventory.Pins.Application.Tags.Commands;

public abstract record OwnerCommand(Guid? OwnerUserId)
{
    public bool IsGlobal => OwnerUserId is null;
}