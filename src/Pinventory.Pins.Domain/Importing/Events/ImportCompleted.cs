using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportCompleted(Guid Id, string UserId, string ArchiveJobId) : DomainEvent(Id);