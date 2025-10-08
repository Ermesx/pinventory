namespace Pinventory.Pins.Import.Events;

public record ImportFailed(Guid ImportJobId, string Error);