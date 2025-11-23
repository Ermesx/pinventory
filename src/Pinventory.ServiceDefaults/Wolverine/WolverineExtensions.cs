using JasperFx;
using JasperFx.CodeGeneration;

using Microsoft.Extensions.DependencyInjection;

using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace Pinventory.ServiceDefaults.Wolverine;

public static class WolverineExtensions
{
    public static WolverineOptions AddDefaultWolverineOptions(this WolverineOptions options)
    {
        if (CodeGeneration.IsGenerating)
        {
            options.Services.DisableAllExternalWolverineTransports();
            options.Services.DisableAllWolverineMessagePersistence();
        }

        options.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource("Wolverine"));

        options.Services.AddJasperFx(x =>
        {
            x.Production.ResourceAutoCreate = AutoCreate.None;
            x.Production.GeneratedCodeMode = TypeLoadMode.Static;
            x.Production.AssertAllPreGeneratedTypesExist = true;
        });

        options.UseEntityFrameworkCoreTransactions();

        options.Policies.UseDurableOutboxOnAllSendingEndpoints();
        options.Policies.UseDurableInboxOnAllListeners();
        options.Policies.UseDurableLocalQueues();
        options.Policies.AutoApplyTransactions();

        return options;
    }

    public static void AddWolverineDebugger(this IServiceCollection services)
    {
        services.AddTransient<WolverineDebugger>();
        services.AddHostedService<WolverineHostedDebugger>();
    }
}