using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public abstract record ImportEvent(Guid Id, string UserId, string ArchiveJobId) : DomainEvent(Id);