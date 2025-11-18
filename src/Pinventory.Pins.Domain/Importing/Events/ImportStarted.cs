using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportStarted(Guid Id, string UserId, string ArchiveJobId) : DomainEvent(Id);