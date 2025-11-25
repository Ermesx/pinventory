namespace Pinventory.Pins.Application.Importing.Messages;

public record PlacesProcessingBatch(Guid ImportId, string UserId, string ArchiveJobId, Guid[] PlaceIds);