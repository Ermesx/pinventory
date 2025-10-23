namespace Pinventory.Pins.Application.Tags.Commands;

public record RemoveTagCommand(string? OwnerId, string Tag) : OwnerCommand(OwnerId);