namespace Pinventory.Pins.Domain.Tagging.Events;

public record TaggingFailed(Guid JobId, string Error);