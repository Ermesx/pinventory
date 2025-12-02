using JasperFx.Core;

using Wolverine;

namespace Pinventory.Pins.Application.Importing.Messages;

public sealed record CheckJobMessage(Guid ImportId, string UserId, string ArchiveJobId) : TimeoutMessage(CheckInterval)
{
    public static readonly TimeSpan CheckInterval = 15.Seconds();
}