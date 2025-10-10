namespace Pinventory.Pins.Verification.Events;

public record VerificationFailed(Guid JobId, string Error);