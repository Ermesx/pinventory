using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Application.Importing.Services.Archive;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure;
using Pinventory.Pins.Infrastructure.Sagas.Messages;

using Wolverine;

namespace Pinventory.Pins.Application.Importing;

// dbContext.SaveChangesAsync() is not used because Wolverine handles transactional outbox 
public sealed class ImportDownloadHandler(
    ILogger<ImportDownloadHandler> logger,
    IImportServiceFactory factory,
    PinsDbContext dbContext,
    IMessageContext bus,
    IArchiveDownloader downloader) : ApplicationHandler(bus)
{
    public async Task HandleAsync(CheckJobMessage check, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking archive job {ArchiveJobId} for {UserId}", check.ArchiveJobId, check.ImportId);
        if (await dbContext.GetCurrentImport(check.ImportId, cancellationToken) is not { } import)
        {
            logger.LogError("Running import {ArchiveJobId} not found for {UserId}", check.ArchiveJobId, check.ImportId);
            return;
        }

        var clientResult = await factory.CreateAsync(import.UserId, cancellationToken);
        if (clientResult.IsFailed)
        {
            logger.LogError("Failed to create import service: {Errors}", clientResult.Errors);
            await ReSchedule();
            return;
        }

        var client = clientResult.Value;
        var archiveResult = await client.CheckJobAsync(check.ArchiveJobId, cancellationToken);
        if (archiveResult.IsFailed)
        {
            logger.LogError("Failed to check archive job: {Errors}", archiveResult.Errors);
            await ReSchedule();
            return;
        }

        switch (archiveResult.Value.State)
        {
            case ImportState.InProgress:
                logger.LogInformation("Archive {ArchiveJobId} is still in progress for {UserId}", check.ArchiveJobId, check.ImportId);
                await ReSchedule();
                return;
            case ImportState.Failed:
                logger.LogWarning("Archive {ArchiveJobId} failed for {UserId}", check.ArchiveJobId, check.ImportId);
                import.Fail(Errors.ImportHandler.ExternalJobFailed());
                break;
            case ImportState.Cancelled:
                logger.LogInformation("Archive {ArchiveJobId} cancelled for {UserId}", check.ArchiveJobId, check.ImportId);
                import.Cancel();
                break;
            default:
                var urls = archiveResult.Value.Urls.Select(x => x.ToString()).ToList();
                await bus.SendAsync(DownloadArchiveMessage.Create(check, urls));
                break;
        }

        await RaiseEventsAsync(import);

        return;

        async Task ReSchedule() => await bus.ScheduleAsync(check with { }, CheckJobMessage.CheckInterval);
    }

    public async Task HandleAsync(DownloadArchiveMessage download, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Downloading archive {Urls}", download.Urls.Select(x => x.ToString()));
        if (await dbContext.GetCurrentImport(download.ImportId, cancellationToken) is not { } import)
        {
            logger.LogError("Running import {ArchiveJobId} not found for {UserId}", download.ArchiveJobId, download.UserId);
            return;
        }

        if (download.Urls.Count < 2)
        {
            logger.LogError("Not enough URLs to download archive");
            import.Fail(Errors.ImportHandler.NotEnoughUrls());
            await RaiseEventsAsync(import);
            return;
        }

        // Relay on Google behavior that the first URL is the data files and the second is the archive browser
        var dataFilesUri = new Uri(download.Urls[0]);
        var archiveBrowserUri = new Uri(download.Urls[1]);

        var dataResult = await downloader.DownloadAsync(archiveBrowserUri, dataFilesUri, cancellationToken);
        if (dataResult.IsFailed)
        {
            logger.LogError("Failed to download archive: {Errors}", dataResult.Errors);
            import.Fail(dataResult.Errors[0]);
            await RaiseEventsAsync(import);
            return;
        }

        var records = dataResult.Value.Data.Features;

        var starredPlaces = records.Select(MapStarredPlace).ToList();
        var result = import.RegisterPlaces(starredPlaces);

        if (result.IsFailed)
        {
            logger.LogError("Failed to register places: {Errors}", result.Errors);
            import.Fail(result.Errors[0]);
        }

        await RaiseEventsAsync(import);

        var batchesCount = (import.Total + ImportProcessingHandler.MaxBatchSize - 1) / ImportProcessingHandler.MaxBatchSize;
        await bus.SendAsync(new ExpectedBatchesMessage(import.Id, import.UserId, import.ArchiveJobId!, batchesCount));

        return;

        static StarredPlace MapStarredPlace(Feature place)
        {
            return new StarredPlace(
                place.Properties.Location?.Name,
                place.Properties.GoogleMapsUrl,
                place.Properties.Location?.Address,
                place.Properties.Location?.CountryCode,
                place.Geometry.Coordinates[1],
                place.Geometry.Coordinates[0],
                place.Properties.Date,
                place.Properties.Comment);
        }
    }
}