using JasperFx;
using JasperFx.CodeGeneration;

using Microsoft.Extensions.DependencyInjection;

using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.MemoryPack;

namespace Pinventory.ServiceDefaults;

public static class WolverineExtensions
{
    public static WolverineOptions AddDefaultWolverineOptions(this WolverineOptions options)
    {
        options.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource("Wolverine"));

        options.Services.AddJasperFx(x =>
        {
            x.Production.ResourceAutoCreate = AutoCreate.None;
            x.Production.GeneratedCodeMode = TypeLoadMode.Static;
            x.Production.AssertAllPreGeneratedTypesExist = true;
        });

        options.UseMemoryPackSerialization();

        options.UseEntityFrameworkCoreTransactions();

        options.Policies.UseDurableOutboxOnAllSendingEndpoints();
        options.Policies.UseDurableInboxOnAllListeners();
        options.Policies.UseDurableLocalQueues();
        options.Policies.ConventionalLocalRoutingIsAdditive();
        options.Policies.AutoApplyTransactions();

        return options;
    }
}