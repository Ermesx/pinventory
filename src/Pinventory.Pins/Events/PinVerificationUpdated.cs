namespace Pinventory.Pins.Events;

public record PinVerificationUpdated(Guid PinId, string ExistsStatus, DateTimeOffset CheckedAt, string Source);