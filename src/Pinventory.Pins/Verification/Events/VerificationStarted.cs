namespace Pinventory.Pins.Verification.Events;

public record VerificationStarted(Guid JobId, string Scope);