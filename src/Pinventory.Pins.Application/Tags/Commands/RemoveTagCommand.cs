using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Application.Tags.Commands;

public record RemoveTagCommand(string? OwnerId, string Tag) : IOwnerCommand;