using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportBatchProcessed(
    Guid Id,
    string UserId,
    string ArchiveJobId,
    int Processed,
    int Created,
    int Updated,
    int Failed,
    int Conflicts,
    int Total)
    : DomainEvent(Id);