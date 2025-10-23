namespace Pinventory.Pins.Domain.Tags.Events;

public record TaggingStarted(Guid JobId, string Scope, string ModelVersion);