namespace Pinventory.Pins.Tagging.Events;

public record TaggingStarted(Guid JobId, string Scope, string ModelVersion);