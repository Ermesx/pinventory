namespace Pinventory.Pins.Import.Events;

public record ImportBatchProcessed(Guid ImportJobId, int Processed, int Created, int Updated, int Failed, int Conflicts);