namespace Pinventory.Pins.Application.Tags.Commands;

public record RemoveTagCommand(Guid? OwnerUserId, string Tag) : OwnerCommand(OwnerUserId);