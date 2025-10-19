namespace Pinventory.Pins.Domain.Import.Events;

public record ImportStarted(Guid ImportJobId, Guid UserId, string Source);