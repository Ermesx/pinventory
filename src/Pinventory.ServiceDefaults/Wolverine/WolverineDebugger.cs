using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Wolverine;

namespace Pinventory.ServiceDefaults.Wolverine;

public class WolverineDebugger(IHostEnvironment env, IMessageBus bus, ILogger<WolverineDebugger> logger)
{
    public void DebugWolverineRouting(params object[] messages)
    {
        if (!env.IsDevelopment())
        {
            return;
        }

        foreach (var message in messages)
        {
            // Preview where Wolverine is wanting to send a message
            var outgoing = bus.PreviewSubscriptions(message);
            foreach (var envelope in outgoing)
            {
                // The URI value here will identify the endpoint where the message is
                // going to be sent (Rabbit MQ exchange, Azure Service Bus topic, Kafka topic, local queue, etc.)
                logger.LogInformation("[Wolverine Debug] Message {Message} is going to {Uri}, RouteInformation: {Route}",
                    message?.GetType(), envelope.Destination, envelope.RoutingInformation);
            }
        }
    }
}