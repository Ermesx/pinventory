using Pinventory.Pins.Application.Commands;
using Pinventory.Pins.Application.Importing.Messages;
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
            new ImportBatchRegistered(Guid.NewGuid(), "test", "job-123", Guid.NewGuid()),
            new ImportBatchProcessed(Guid.NewGuid(), "test", "job-123", 1, 1, 1, 1, 1),
            new ImportProcessCompleted(Guid.NewGuid(), "test", "job-123"),
            new AssignTags(Guid.NewGuid(), new List<string>(), "test", 1),
            new ImportBatchesCleared(Guid.NewGuid(), "test", "job-123"),
            new ImportCancelled(Guid.NewGuid(), "test", "job-123"),
            new ImportFailed(Guid.NewGuid(), "test", "job-123", "test"),
            new ImportProcessTimeout(Guid.NewGuid())
        ]);
    }
}