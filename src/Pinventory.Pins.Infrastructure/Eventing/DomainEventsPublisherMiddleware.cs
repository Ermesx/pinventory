using Pinventory.Pins.Domain.Abstractions;

using Wolverine;

namespace Pinventory.Pins.Infrastructure.Eventing;

public class DomainEventsPublisherMiddleware
{
    public OutgoingMessages PostProcessAsync(PinsDbContext dbContext) =>
    [
        dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList()
    ];
}