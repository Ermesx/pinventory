namespace Pinventory.Pins.Infrastructure.Sagas.Messages;

public record ExpectedBatchesMessage(Guid Id, string UserId, string ArchiveJobId, int BatchesCount);