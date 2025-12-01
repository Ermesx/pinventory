namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportCompleted(Guid Id, string UserId, string ArchiveJobId) : ImportEvent(Id, UserId, ArchiveJobId);