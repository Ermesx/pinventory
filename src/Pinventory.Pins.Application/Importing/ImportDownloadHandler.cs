using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure.Sagas.Messages;

namespace Pinventory.Pins.Application.Importing;

// dbContext.SaveChangesAsync() is not used because Wolverine handles transactional outbox 
public sealed class ImportDownloadHandler(
    ILogger<ImportDownloadHandler> logger,
    IImportServiceFactory factory,
    IStarredPlacesProvider placesProvider)
{
    public async Task<(DownloadArchiveMessage? DownloadMessage, CheckJobMessage? CheckJobMessage)> HandleAsync(
        CheckJobMessage check,
        Import? import,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking archive job {ImportId} for {UserId}", check.ImportId, check.UserId);
        if (import is null)
        {
            logger.LogError("Running import {ImportId} not found for {UserId}", check.ImportId, check.UserId);
            return (null, null);
        }

        var clientResult = await factory.CreateAsync(import.UserId, cancellationToken);
        if (clientResult.IsFailed)
        {
            logger.LogError("Failed to create import service: {Errors}", clientResult.Errors);
            return (null, check);
        }

        var client = clientResult.Value;
        var archiveResult = await client.CheckJobAsync(check.ArchiveJobId, cancellationToken);
        if (archiveResult.IsFailed)
        {
            logger.LogError("Failed to check archive job: {Errors}", archiveResult.Errors);
            return (null, check);
        }

        switch (archiveResult.Value.State)
        {
            case ImportState.InProgress:
                logger.LogInformation("Archive {ImportId} is still in progress for {UserId}", check.ImportId, check.ImportId);
                return (null, check);
            case ImportState.Failed:
                logger.LogWarning("Archive {ImportId} failed for {UserId}", check.ImportId, check.ImportId);
                import.Fail(Errors.ImportHandler.ExternalJobFailed());
                return (null, null);
            case ImportState.Cancelled:
                logger.LogInformation("Archive {ImportId} cancelled for {UserId}", check.ImportId, check.ImportId);
                import.Cancel();
                return (null, null);
        }

        var urls = archiveResult.Value.Urls.Select(x => x.ToString()).ToList();
        return (DownloadArchiveMessage.Create(check, urls), null);
    }

    public async Task<ExpectedBatchesMessage?> HandleAsync(
        DownloadArchiveMessage download,
        Import? import,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Downloading archive {Urls}", download.Urls.Select(x => x.ToString()));
        if (import is null)
        {
            logger.LogError("Running import {ImportId} not found for {UserId}", download.ImportId, download.UserId);
            return null;
        }

        var uris = download.Urls.Select(x => new Uri(x)).ToList();
        var dataResult = await placesProvider.ProvideAsync(uris, cancellationToken);
        if (dataResult.IsFailed)
        {
            logger.LogError("Failed to download archive: {Errors}", dataResult.Errors);
            import.Fail(dataResult.Errors[0]);
            return null;
        }

        var starredPlaces = dataResult.Value;
        var result = import.RegisterPlaces(starredPlaces);

        if (result.IsFailed)
        {
            if (result.HasError<Domain.Errors.Import.EmptyPlacesError>())
            {
                logger.LogWarning("No places to import for {ImportId} for {UserId}", import.Id, import.UserId);
                import.Complete();
                return null;
            }

            logger.LogError("Failed to register places: {Errors}", result.Errors);
            import.Fail(result.Errors.First());
            return null;
        }

        var batchesCount = (import.Total + ImportProcessingHandler.MaxBatchSize - 1) / ImportProcessingHandler.MaxBatchSize;
        return new ExpectedBatchesMessage(import.Id, import.UserId, import.ArchiveJobId!, batchesCount);
    }
}