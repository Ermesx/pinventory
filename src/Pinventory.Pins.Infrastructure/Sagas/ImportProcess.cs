using Microsoft.Extensions.Logging;

using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Infrastructure.Sagas.Messages;

using Wolverine;

namespace Pinventory.Pins.Infrastructure.Sagas;

public class ImportProcess : Saga
{
    public Guid? Id { get; init; }

    public int TotalBatches { get; private set; }

    public int BatchesProcessed { get; private set; }

    public static (ImportProcess, ImportProcessTimeout) Start(ImportStarted @event, ILogger<ImportProcess> logger)
    {
        logger.LogInformation("Import process [Saga] created for {ArchiveJobId} for {UserId}", @event.ArchiveJobId, @event.UserId);

        return (new ImportProcess { Id = @event.Id }, new ImportProcessTimeout(@event.Id));
    }

    public void Handle(ImportBatchRegistered @event, ILogger<ImportProcess> logger)
    {
        logger.LogInformation("Batch added to Import process [Saga] for {ArchiveJobId} for {UserId}", @event.ArchiveJobId, @event.UserId);

        TotalBatches++;
    }

    public ImportProcessCompleted? Handle(ImportBatchProcessed @event, ILogger<ImportProcess> logger)
    {
        logger.LogInformation("Batch processed added to Import process [Saga] for {ArchiveJobId} for {UserId}", @event.ArchiveJobId,
            @event.UserId);

        BatchesProcessed++;

        if (BatchesProcessed < TotalBatches)
        {
            return null;
        }

        logger.LogInformation("Import process [Saga] completed for {ArchiveJobId} for {UserId}", @event.ArchiveJobId, @event.UserId);

        MarkCompleted();

        return new ImportProcessCompleted(@event.Id, @event.ArchiveJobId, @event.UserId);
    }

    public void Handle(ImportBatchesCleared @event, ILogger<ImportProcess> logger)
    {
        logger.LogInformation("Import process [Saga] cleared for {ArchiveJobId} for {UserId}", @event.ArchiveJobId, @event.UserId);

        TotalBatches = 0;
        BatchesProcessed = 0;
    }

    public void Handle(ImportFailed @event, ILogger<ImportProcess> logger)
    {
        logger.LogInformation("Import process [Saga] failed for {ArchiveJobId} for {UserId}", @event.ArchiveJobId, @event.UserId);

        MarkCompleted();
    }

    public void Handle(ImportCancelled @event, ILogger<ImportProcess> logger)
    {
        logger.LogInformation("Import process [Saga] cancelled for {ArchiveJobId} for {UserId}", @event.ArchiveJobId, @event.UserId);

        MarkCompleted();
    }

    public void Handle(ImportProcessTimeout timeout, ILogger<ImportProcess> logger)
    {
        logger.LogInformation("Import process [Saga] timed out for {ImportId}", timeout.ImportId);

        MarkCompleted();
    }
}