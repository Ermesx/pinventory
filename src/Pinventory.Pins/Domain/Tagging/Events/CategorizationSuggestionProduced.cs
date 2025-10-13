namespace Pinventory.Pins.Domain.Tagging.Events;

public record TaggingSuggestionProduced(Guid JobId, Guid PinId, string[] Tags, double Confidence);