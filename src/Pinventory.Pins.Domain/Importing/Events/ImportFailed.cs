using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportFailed(Guid AggregateId, string UserId, string ArchiveJobId, string Error) : DomainEvent(AggregateId);