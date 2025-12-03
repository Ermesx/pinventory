using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.ServiceDefaults.Wolverine;

namespace Pinventory.Pins.Api;

public static class DebugWolverineExtensions
{
    public static void AddDebugWolverineRouting(this IServiceCollection services)
    {
        services.AddWolverineDebugger();

        services.AddSingleton<MessagesProvider>(() =>
        [
            new StartImportCommand("test", null, null),
            new RenewImportCommand("test", Guid.NewGuid()),
            new CancelImportCommand("test", Guid.NewGuid()),

            new DefineTagCatalogCommand("test", new List<string>()),
            new AddTagCommand("test", "tag"),
            new RemoveTagCommand("test", "tag")
        ]);
    }
}