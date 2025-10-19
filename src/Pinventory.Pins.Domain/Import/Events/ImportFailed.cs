namespace Pinventory.Pins.Domain.Import.Events;

public record ImportFailed(Guid ImportJobId, string Error);