using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportBatchRegistered(Guid Id, string UserId, string? ArchiveJobId, Guid BatchId) : DomainEvent(Id);