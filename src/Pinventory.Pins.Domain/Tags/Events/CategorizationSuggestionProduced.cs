namespace Pinventory.Pins.Domain.Tags.Events;

public record TaggingSuggestionProduced(Guid JobId, Guid PinId, string[] Tags, double Confidence);