namespace Pinventory.Pins.Domain.Tagging.Events;

public record TaggingStarted(Guid JobId, string Scope, string ModelVersion);