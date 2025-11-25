namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportPlaceRegistered(Guid Id, string UserId, string ArchiveJobId, Guid PlaceId)
    : ImportEvent(Id, UserId, ArchiveJobId);