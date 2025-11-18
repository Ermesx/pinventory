using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportCancelled(Guid Id, string UserId, string ArchiveJobId) : DomainEvent(Id);