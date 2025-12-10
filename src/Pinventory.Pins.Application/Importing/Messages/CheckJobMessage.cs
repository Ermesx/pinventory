using JasperFx.Core;

using Pinventory.Pins.Infrastructure.Messages;

using Wolverine;

namespace Pinventory.Pins.Application.Importing.Messages;

public sealed record CheckJobMessage(Guid ImportId, string UserId, string ArchiveJobId)
    : TimeoutMessage(CheckInterval), IUserMessage
{
    public static readonly TimeSpan CheckInterval = 15.Seconds();
}