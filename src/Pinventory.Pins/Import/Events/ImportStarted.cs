namespace Pinventory.Pins.Import.Events;

public record ImportStarted(Guid ImportJobId, Guid UserId, string Source);