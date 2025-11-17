using JasperFx.Core;

namespace Pinventory.Pins.Application.Importing.Messages;

// Check every minute
public record CheckJobMessage(Guid ImportId, string UserId, string ArchiveJobId)
{
    public static readonly TimeSpan CheckInterval = 15.Seconds();
}