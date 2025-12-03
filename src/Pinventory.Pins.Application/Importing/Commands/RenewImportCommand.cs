using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Application.Importing.Commands;

public record RenewImportCommand(string UserId, Guid ImportId) : IUserMessage;