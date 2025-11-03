using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Import.Events;

public record ImportStarted(Guid AggregateId, string UserId, string ArchiveJobId) : DomainEvent(AggregateId);