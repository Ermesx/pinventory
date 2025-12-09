namespace Pinventory.Pins.Infrastructure.Importing.Sagas.Messages;

public record ExpectedBatchesMessage(Guid Id, string UserId, string ArchiveJobId, int BatchesCount);