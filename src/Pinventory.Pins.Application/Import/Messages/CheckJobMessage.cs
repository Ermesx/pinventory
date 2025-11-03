using Wolverine;

namespace Pinventory.Pins.Application.Import.Messages;

// Check every minute
public record CheckJobMessage(string UserId, string ArchiveJobId) : TimeoutMessage(TimeSpan.FromMinutes(1));