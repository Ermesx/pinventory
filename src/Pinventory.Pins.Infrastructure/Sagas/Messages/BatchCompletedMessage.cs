namespace Pinventory.Pins.Infrastructure.Sagas.Messages;

public record BatchCompletedMessage(Guid Id, string UserId, string ArchiveJobId);