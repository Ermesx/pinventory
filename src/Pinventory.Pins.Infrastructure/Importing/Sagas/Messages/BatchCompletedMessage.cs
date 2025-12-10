namespace Pinventory.Pins.Infrastructure.Importing.Sagas.Messages;

public record BatchCompletedMessage(Guid Id, string UserId, string ArchiveJobId);