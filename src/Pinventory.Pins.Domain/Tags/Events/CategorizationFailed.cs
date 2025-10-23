namespace Pinventory.Pins.Domain.Tags.Events;

public record TaggingFailed(Guid JobId, string Error);