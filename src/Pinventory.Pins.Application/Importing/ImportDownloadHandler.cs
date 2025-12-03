using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure;
using Pinventory.Pins.Infrastructure.Sagas.Messages;

namespace Pinventory.Pins.Application.Importing;

// dbContext.SaveChangesAsync() is not used because Wolverine handles transactional outbox 
public sealed class ImportDownloadHandler(
    ILogger<ImportDownloadHandler> logger,
    IImportServiceFactory factory,
    PinsDbContext dbContext,
    IStarredPlacesProvider placesProvider)
{
    public async Task<(DownloadArchiveMessage? DownloadMessage, CheckJobMessage? CheckJobMessage)> HandleAsync(CheckJobMessage check,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking archive job {ArchiveJobId} for {UserId}", check.ArchiveJobId, check.ImportId);
        if (await dbContext.GetCurrentImport(check.ImportId, cancellationToken) is not { } import)
        {
            logger.LogError("Running import {ArchiveJobId} not found for {UserId}", check.ArchiveJobId, check.ImportId);
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
                logger.LogInformation("Archive {ArchiveJobId} is still in progress for {UserId}", check.ArchiveJobId, check.ImportId);
                return (null, check);
            case ImportState.Failed:
                logger.LogWarning("Archive {ArchiveJobId} failed for {UserId}", check.ArchiveJobId, check.ImportId);
                import.Fail(Errors.ImportHandler.ExternalJobFailed());
                break;
            case ImportState.Cancelled:
                logger.LogInformation("Archive {ArchiveJobId} cancelled for {UserId}", check.ArchiveJobId, check.ImportId);
                import.Cancel();
                break;
        }

        var urls = archiveResult.Value.Urls.Select(x => x.ToString()).ToList();
        return (DownloadArchiveMessage.Create(check, urls), null);
    }

    public async Task<ExpectedBatchesMessage?> HandleAsync(DownloadArchiveMessage download, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Downloading archive {Urls}", download.Urls.Select(x => x.ToString()));
        if (await dbContext.GetCurrentImport(download.ImportId, cancellationToken) is not { } import)
        {
            logger.LogError("Running import {ArchiveJobId} not found for {UserId}", download.ArchiveJobId, download.UserId);
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