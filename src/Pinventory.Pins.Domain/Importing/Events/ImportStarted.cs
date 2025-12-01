namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportStarted(Guid Id, string UserId, string ArchiveJobId) : ImportEvent(Id, UserId, ArchiveJobId);