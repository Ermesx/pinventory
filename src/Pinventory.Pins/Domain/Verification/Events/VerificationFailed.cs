namespace Pinventory.Pins.Domain.Verification.Events;

public record VerificationFailed(Guid JobId, string Error);