using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Pinventory.ServiceDefaults.Wolverine;

public delegate object[] MessagesProvider();

public class WolverineHostedDebugger(IServiceProvider serviceProvider, MessagesProvider messagesProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var debugger = scope.ServiceProvider.GetRequiredService<WolverineDebugger>();
        debugger.DebugWolverineRouting(messagesProvider());
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}