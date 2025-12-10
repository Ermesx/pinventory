using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Infrastructure.Importing.Messages;

public record PlacesProcessingBatchMessage(Guid ImportId, string UserId, string ArchiveJobId, Guid[] PlaceIds) : IUserMessage;