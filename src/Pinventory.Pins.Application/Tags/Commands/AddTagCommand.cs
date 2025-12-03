using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Application.Tags.Commands;

public record AddTagCommand(string? OwnerId, string Tag) : IOwnerCommand;