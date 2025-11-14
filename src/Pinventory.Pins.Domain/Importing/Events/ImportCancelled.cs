using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportCancelled(Guid AggregateId, string UserId, string ArchiveJobId) : DomainEvent(AggregateId);