using Pinventory.Pins.Domain.Abstractions;

namespace Pinventory.Pins.Domain.Importing.Events;

public sealed record ImportFailed(Guid Id, string UserId, string ArchiveJobId, string Error) : DomainEvent(Id);