namespace Pinventory.Pins.Domain.Importing.Events;

public record ImportPlacesCleared(Guid Id, string UserId, string ArchiveJobId) : ImportEvent(Id, UserId, ArchiveJobId);