using Pinventory.Pins.Application.Importing.Messages;

namespace Pinventory.Pins.Application.Importing.Commands;

public record CancelImportCommand(string UserId, string ArchiveJobId) : ICorrelatedMessage;