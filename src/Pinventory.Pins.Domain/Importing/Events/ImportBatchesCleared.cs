using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public record ImportBatchesCleared(Guid Id, string UserId, string ArchiveJobId) : DomainEvent(Id);