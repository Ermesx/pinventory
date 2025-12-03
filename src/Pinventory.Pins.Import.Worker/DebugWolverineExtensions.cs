using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Tagging.Messages;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Infrastructure.Sagas.Messages;
using Pinventory.ServiceDefaults.Wolverine;

namespace Pinventory.Pins.Import.Worker;

public static class DebugWolverineExtensions
{
    public static void AddDebugWolverineRouting(this IServiceCollection services)
    {
        services.AddWolverineDebugger();

        services.AddSingleton<MessagesProvider>(() =>
        [
            new ImportStarted(Guid.NewGuid(), "test", "job-123"),

            new CheckJobMessage(Guid.NewGuid(), "test", "job-123"),
            new DownloadArchiveMessage(Guid.NewGuid(), "test", "job-123", new List<string>()),

            new ImportPlaceRegistered(Guid.NewGuid(), "test", "job-123", Guid.NewGuid()),
            new ImportPlaceProcessed(Guid.NewGuid(), "test", "job-123", Guid.NewGuid(), StarredPlaceState.New),

            new AssignTagsToPinMessage(Guid.NewGuid()),

            new ImportPlacesCleared(Guid.NewGuid(), "test", "job-123"),
            new ImportCancelled(Guid.NewGuid(), "test", "job-123"),
            new ImportFailed(Guid.NewGuid(), "test", "job-123", "test"),

            new ImportProcessTimeout(Guid.NewGuid(), "test"),
            new ImportProcessCompleted(Guid.NewGuid(), "test", "job-123")
        ]);
    }
}