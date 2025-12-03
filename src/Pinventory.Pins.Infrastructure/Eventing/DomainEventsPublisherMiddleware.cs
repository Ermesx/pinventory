using Pinventory.Pins.Domain.Abstractions;

using Wolverine;

namespace Pinventory.Pins.Infrastructure.Eventing;

public class DomainEventsPublisherMiddleware
{
    public async Task PostProcessAsync(PinsDbContext dbContext, IMessageBus bus)
    {
        foreach (var @event in dbContext.ChangeTracker
                     .Entries<AggregateRoot>()
                     .SelectMany(x => x.Entity.DomainEvents)
                     .ToList())
        {
            await bus.PublishAsync(@event);
        }
    }
}