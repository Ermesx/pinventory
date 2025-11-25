namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportFailed(Guid Id, string UserId, string ArchiveJobId, string Error) : ImportEvent(Id, UserId, ArchiveJobId);