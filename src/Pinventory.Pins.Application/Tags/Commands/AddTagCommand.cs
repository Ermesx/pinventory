namespace Pinventory.Pins.Application.Tags.Commands;

public record AddTagCommand(Guid? OwnerUserId, string Tag) : OwnerCommand(OwnerUserId);