using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Infrastructure.Importing.Sagas.Messages;

public record ImportProcessCompleted(Guid ImportId, string ArchiveJobId, string UserId) : IUserMessage;