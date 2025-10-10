namespace Pinventory.Pins.Events;

public record PinTagsAssigned(Guid PinId, string[] Tags, string Reason);