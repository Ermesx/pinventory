namespace Pinventory.Pins.Domain.Verification.Events;

public record VerificationStarted(Guid JobId, string Scope);