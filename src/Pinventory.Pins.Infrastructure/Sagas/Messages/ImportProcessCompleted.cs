using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Infrastructure.Sagas.Messages;

public record ImportProcessCompleted(Guid ImportId, string ArchiveJobId, string UserId) : IUserMessage;