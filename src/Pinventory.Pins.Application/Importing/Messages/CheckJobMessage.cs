using Wolverine;

namespace Pinventory.Pins.Application.Importing.Messages;

// Check every minute
public record CheckJobMessage(string UserId, string ArchiveJobId) : TimeoutMessage(TimeSpan.FromMinutes(1)), ICorrelatedMessage
{
    public static CheckJobMessage Create(ICorrelatedMessage message) => new(message.UserId, message.ArchiveJobId);
}