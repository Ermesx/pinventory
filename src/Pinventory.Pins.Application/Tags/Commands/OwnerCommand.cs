namespace Pinventory.Pins.Application.Tags.Commands;

// TODO: remove OwnerCommand and use IUserMessage
public abstract record OwnerCommand(string? OwnerId)
{
    public bool IsGlobal => OwnerId is null;
}