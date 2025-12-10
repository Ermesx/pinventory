using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Application.Importing.Commands;

public record StartImportCommand(string UserId, DateTimeOffset? Start, DateTimeOffset? End) : IUserMessage;