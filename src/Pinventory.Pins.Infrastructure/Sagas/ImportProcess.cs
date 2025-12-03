using Microsoft.Extensions.Logging;

using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Infrastructure.Sagas.Messages;

using Wolverine;

namespace Pinventory.Pins.Infrastructure.Sagas;

public class ImportProcess : Saga
{
    public Guid Id { get; init; }

    public int BatchesToProceed { get; private set; }

    public static (ImportProcess, ImportProcessTimeout) Start(ImportStarted @event, ILogger<ImportProcess> logger)
    {
        logger.LogInformation("Import process [Saga] created for {ArchiveJobId} for {UserId}", @event.ArchiveJobId, @event.UserId);

        return (new ImportProcess { Id = @event.Id }, new ImportProcessTimeout(@event.Id));
    }

    public void Handle(ExpectedBatchesMessage message, ILogger<ImportProcess> logger)
    {
        logger.LogInformation("Places added to Import process [Saga] for {ArchiveJobId} for {UserId}", @message.ArchiveJobId,
            @message.UserId);

        BatchesToProceed = message.BatchesCount;
    }

    public ImportProcessCompleted Handle(BatchCompletedMessage message, ILogger<ImportProcess> logger)
    {
        logger.LogInformation("Places processed added to Import process [Saga] for {ArchiveJobId} for {UserId}", message.ArchiveJobId,
            message.UserId);

        BatchesToProceed--;
        if (BatchesToProceed != 0)
        {
            return null!;
        }

        logger.LogInformation("Import process [Saga] completed for {ArchiveJobId} for {UserId}", message.ArchiveJobId, message.UserId);

        MarkCompleted();

        return new ImportProcessCompleted(message.Id, message.ArchiveJobId, message.UserId);
    }

    public void Handle(ImportPlacesCleared @event, ILogger<ImportProcess> logger)
    {
        logger.LogInformation("Import process [Saga] cleared for {ArchiveJobId} for {UserId}", @event.ArchiveJobId, @event.UserId);

        BatchesToProceed = 0;
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

    // TODO: Add NotFound to handle timeouts when import is completed
}