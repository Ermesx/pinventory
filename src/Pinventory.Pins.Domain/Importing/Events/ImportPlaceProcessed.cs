namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportPlaceProcessed(Guid Id, string UserId, string ArchiveJobId, Guid PlaceId, StarredPlaceState PlaceState)
    : ImportEvent(Id, UserId, ArchiveJobId);