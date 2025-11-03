using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Import.Events;

public record ImportBatchProcessed(Guid AggregateId, int Processed, int Created, int Updated, int Failed, int Conflicts)
    : DomainEvent(AggregateId);