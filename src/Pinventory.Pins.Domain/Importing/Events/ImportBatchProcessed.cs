using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public record ImportBatchProcessed(
    Guid AggregateId,
    string UserId,
    string ArchiveJobId,
    int Processed,
    int Created,
    int Updated,
    int Failed,
    int Conflicts,
    uint Total)
    : DomainEvent(AggregateId);