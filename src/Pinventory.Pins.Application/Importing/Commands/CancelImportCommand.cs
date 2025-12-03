using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Application.Importing.Commands;

public record CancelImportCommand(string UserId, Guid ImportId) : IUserMessage;