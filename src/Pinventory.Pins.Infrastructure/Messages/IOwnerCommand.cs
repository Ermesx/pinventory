namespace Pinventory.Pins.Infrastructure.Messages;

public interface IOwnerCommand
{
    string? OwnerId { get; }

    bool IsGlobal => OwnerId is null;
}