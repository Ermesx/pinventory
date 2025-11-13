using JasperFx.Core;

using Wolverine;

namespace Pinventory.Pins.Application.Importing.Messages;

// Check every minute
public record CheckJobMessage(string UserId, string ArchiveJobId) : TimeoutMessage(CheckInterval), ICorrelatedMessage
{
    public static readonly TimeSpan CheckInterval = 10.Seconds();
    public static CheckJobMessage Create(ICorrelatedMessage message) => new(message.UserId, message.ArchiveJobId);
}