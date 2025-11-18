using FluentResults;

using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Application.Importing.Services.Archive;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure;

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
    private const int BatchSize = 50;

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
            return;
        }

        var client = clientResult.Value;
        var archiveResult = await client.CheckJobAsync(check.ArchiveJobId, cancellationToken);
        if (archiveResult.IsFailed)
        {
            logger.LogError("Failed to check archive job: {Errors}", archiveResult.Errors);
            return;
        }

        switch (archiveResult.Value.State)
        {
            case ImportState.InProgress:
                logger.LogInformation("Archive {ArchiveJobId} is still in progress for {UserId}", check.ArchiveJobId, check.ImportId);
                await bus.ScheduleAsync(check with { }, CheckJobMessage.CheckInterval);
                return;
            case ImportState.Failed:
                logger.LogWarning("Archive {ArchiveJobId} failed for {UserId}", check.ArchiveJobId, check.ImportId);
                if (import.Fail(new Error("Archive job failed externally")) is { IsFailed: true } failResult)
                {
                    logger.LogError("Failed to fail import job: {Errors}", failResult.Errors);
                    return;
                }

                break;
            case ImportState.Cancelled:
                logger.LogInformation("Archive {ArchiveJobId} cancelled for {UserId}", check.ArchiveJobId, check.ImportId);
                if (import.Cancel() is { IsFailed: true } cancelResult)
                {
                    logger.LogError("Failed to cancel import job: {Errors}", cancelResult.Errors);
                    return;
                }

                break;
            default:
                var urls = archiveResult.Value.Urls.Select(x => x.ToString()).ToList();
                await bus.PublishAsync(DownloadArchiveMessage.Create(check, urls));
                break;
        }

        await RaiseEventsAsync(import);
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
            return;
        }

        // Relay on Google behavior that the first URL is the data files and the second is the archive browser
        var dataFilesUri = new Uri(download.Urls[0]);
        var archiveBrowserUri = new Uri(download.Urls[1]);

        var dataResult = await downloader.DownloadAsync(archiveBrowserUri, dataFilesUri, cancellationToken);
        if (dataResult.IsFailed)
        {
            logger.LogError("Failed to download archive: {Errors}", dataResult.Errors);
            return;
        }

        var records = dataResult.Value.Data.Features;

        List<Result<Success>> results = [];
        foreach (var batch in records.Chunk(BatchSize))
        {
            var starredPlaces = batch.Select(MapStarredPlace).ToList();
            results.Add(import.RegisterBatch(starredPlaces));
        }

        if (results.Any(x => x.IsFailed))
        {
            logger.LogError("Failed to download archive: {Errors}", results.SelectMany(x => x.Errors));
        }

        await RaiseEventsAsync(import);

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