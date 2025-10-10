namespace Pinventory.Pins.Tagging.Events;

public record TaggingFailed(Guid JobId, string Error);