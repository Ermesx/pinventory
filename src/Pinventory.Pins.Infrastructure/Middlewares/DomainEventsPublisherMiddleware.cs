using Pinventory.Pins.Domain.Abstractions;

using Wolverine;

namespace Pinventory.Pins.Infrastructure.Middlewares;

public class DomainEventsPublisherMiddleware
{
    public async Task PostProcessAsync(PinsDbContext dbContext, IMessageBus bus)
    {
        var aggregates = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(x => x.Entity)
            .Where(x => x.DomainEvents.Count > 0)
            .ToList();

        foreach (var @event in aggregates.SelectMany(x => x.DomainEvents).ToList())
        {
            await bus.PublishAsync(@event);
        }

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }
}