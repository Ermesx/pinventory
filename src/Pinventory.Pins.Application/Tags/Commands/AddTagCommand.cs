namespace Pinventory.Pins.Application.Tags.Commands;

public record AddTagCommand(string? OwnerId, string Tag) : OwnerCommand(OwnerId);