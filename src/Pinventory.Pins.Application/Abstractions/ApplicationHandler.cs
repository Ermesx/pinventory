using Pinventory.Pins.Abstractions;

using Wolverine;

namespace Pinventory.Pins.Application.Abstractions;

public abstract class ApplicationHandler(IMessageBus bus)
{
    protected async Task RaiseEvents(AggregateRoot aggregateRoot)
    {
        foreach (var @event in aggregateRoot.DomainEvents)
        {
            await bus.PublishAsync(@event);
        }
    }
}