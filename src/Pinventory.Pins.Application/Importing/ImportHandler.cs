using FluentResults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Application.Importing.Services.Archive;
using Pinventory.Pins.Application.Tagging.Messages;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Infrastructure;

using Wolverine;

namespace Pinventory.Pins.Application.Importing;

public sealed class ImportHandler(
    ILogger<ImportHandler> logger,
    IImportServiceFactory factory,
    PinsDbContext dbContext,
    IMessageContext bus,
    IImportConcurrencyPolicy concurrencyPolicy,
    IArchiveDownloader downloader) : ApplicationHandler(bus)
{
    private const int BatchSize = 50;

    private const string RemovedPlaceComment = "No location information is available for this saved place";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    public async Task<Result<string>> HandleAsync(StartImportCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting importMessage for {UserId}", command.UserId);

        var client = await CreateClientAsync(command.UserId);
        var archiveJobId = await client.InitiateAsync(command.Period, cancellationToken);

        var import = new Import(command.UserId, command.Period ?? Period.AllTime);
        var result = await import.StartAsync(archiveJobId, concurrencyPolicy);

        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        await dbContext.Imports.AddAsync(import, cancellationToken);
        await RaiseEventsAsync(import);

        await bus.PublishAsync(new CheckJobMessage(import.UserId, archiveJobId));

        return Result.Ok(archiveJobId);
    }

    public async Task<Result<Success>> HandleAsync(CancelImportCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Cancelling import '{ArchiveJobId}' for {UserId}", command.ArchiveJobId, command.UserId);

        var import = await GetCurrentImport(command, cancellationToken);
        if (import is null)
        {
            return Result.Fail(Errors.Import.RunningImportNotFound(command));
        }

        var result = import.Cancel();
        if (result.IsSuccess)
        {
            var client = await CreateClientAsync(command.UserId);
            await client.CancelJobAsync(command.ArchiveJobId, cancellationToken);
        }

        await RaiseEventsAsync(import);
        return result;
    }

    public async Task HandleAsync(CheckJobMessage check, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking archive job {ArchiveJobId} for {UserId}", check.ArchiveJobId, check.UserId);

        var import = await GetCurrentImport(check, cancellationToken);
        if (import is null)
        {
            logger.LogError("Running import {ArchiveJobId} not found for {UserId}", check.ArchiveJobId, check.UserId);
            return;
        }

        var client = await CreateClientAsync(check.UserId);
        var archiveResult = await client.CheckJobAsync(check.ArchiveJobId, cancellationToken);

        Result<Success> result;
        switch (archiveResult.State)
        {
            case ImportState.InProgress:
                logger.LogInformation("Archive {ArchiveJobId} is still in progress for {UserId}", check.ArchiveJobId, check.UserId);
                await bus.ReScheduleCurrentAsync(DateTimeOffset.UtcNow.Add(CheckInterval));
                return;
            case ImportState.Failed:
                logger.LogWarning("Archive {ArchiveJobId} failed for {UserId}", check.ArchiveJobId, check.UserId);
                result = import.Fail("Archive job failed");
                if (result.IsFailed)
                {
                    logger.LogError("Failed to fail import job: {Errors}", result.Errors);
                    return;
                }

                break;
            case ImportState.Cancelled:
                logger.LogInformation("Archive {ArchiveJobId} cancelled for {UserId}", check.ArchiveJobId, check.UserId);
                result = import.Cancel();
                if (result.IsFailed)
                {
                    logger.LogError("Failed to cancel import job: {Errors}", result.Errors);
                    return;
                }

                break;
            default:
                var urls = archiveResult.Urls.Select(x => x.ToString()).ToList();
                await bus.PublishAsync(DownloadArchiveMessage.Create(check, urls));
                break;
        }

        await RaiseEventsAsync(import);
    }

    public async Task HandleAsync(DownloadArchiveMessage download, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Downloading archive {Urls}", download.Urls.Select(x => x.ToString()));

        var import = await GetCurrentImport(download, cancellationToken);
        if (import is null)
        {
            logger.LogError("Running import {ArchiveJobId} not found for {UserId}", download.ArchiveJobId, download.UserId);
            return;
        }

        if (download.Urls.Count < 2)
        {
            logger.LogError("Not enough URLs to download archive");
            return;
        }

        var archiveBrowserUri = new Uri(download.Urls[0]);
        var dataFilesUri = new Uri(download.Urls[1]);

        var result = await downloader.DownloadAsync(archiveBrowserUri, dataFilesUri, cancellationToken);
        if (result.IsFailed)
        {
            logger.LogError("Failed to download archive: {Errors}", result.Errors);
            return;
        }

        var records = result.Value.Data.Features;
        import.UpdateTotal((uint)records.Length);

        foreach (var batch in records.Chunk(BatchSize))
        {
            var starredPlaces = batch.Select(MapStarredPlace).ToList();
            await bus.PublishAsync(ProcessPinsBatchMessage.Create(download, starredPlaces));
        }

        return;

        static StarredPlace MapStarredPlace(Feature place)
        {
            return new StarredPlace(
                place.Properties.Location?.Name,
                place.Properties.GoogleMapsUrl,
                place.Properties.Location?.Address,
                place.Properties.Location?.CountryCode,
                place.Geometry.Coordinates[0],
                place.Geometry.Coordinates[1],
                place.Properties.Date,
                place.Properties.Comment);
        }
    }

    public async Task HandleAsync(ProcessPinsBatchMessage batch, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Import {ArchiveJobId}: Processing batch of {Count} pins for {UserId}", batch.ArchiveJobId,
            batch.StarredPlaces.Count(), batch.UserId);

        var import = await GetCurrentImport(batch, cancellationToken);
        if (import is null)
        {
            logger.LogError("Running import {ArchiveJobId} not found for {UserId}", batch.ArchiveJobId, batch.UserId);
            return;
        }

        int processed = 0, created = 0, updated = 0, failed = 0, conflicts = 0;
        List<ReportedPlace> conflictingPlaces = [], failedPlaces = [];
        List<Pin> pinsToCreate = [], updatedPins = [];

        var existingPins = dbContext.Pins.Where(x => x.OwnerId == batch.UserId).ToList();
        foreach (var place in batch.StarredPlaces)
        {
            processed++;

            if (place.Comment == RemovedPlaceComment)
            {
                failedPlaces.Add(new ReportedPlace(place.GoogleMapsUrl, place.AddedDate));
                failed++;
                continue;
            }

            // Conflicted places are those with the same name but different place ID
            var possibleConflictedPin = existingPins.SingleOrDefault(x => x.Name == place.Name);
            if (possibleConflictedPin is not null)
            {
                conflictingPlaces.Add(new ReportedPlace(place.GoogleMapsUrl, place.AddedDate));
                conflicts++;
                continue;
            }

            var placeId = GooglePlaceId.Parse(place.GoogleMapsUrl);
            var existingPin = existingPins.FirstOrDefault(x => x.PlaceId == placeId);
            if (existingPin is null)
            {
                pinsToCreate.Add(CreatePin(placeId, place));
                created++;
                continue;
            }

            existingPin.Rename(place.Name!);
            updatedPins.Add(existingPin);
            updated++;
        }

        var appendBatch = import.AppendBatch(processed, created, updated, failed, conflicts);
        if (appendBatch.IsFailed)
        {
            logger.LogError("Failed to append batch: {Errors}", appendBatch.Errors);
            return;
        }

        import.ReportConflictsAndFailures(conflictingPlaces, failedPlaces);

        var tryComplete = import.TryComplete();
        if (tryComplete.IsFailed)
        {
            logger.LogError("Failed to complete import job: {Errors}", tryComplete.Errors);
            return;
        }

        if (tryComplete.Value)
        {
            logger.LogInformation("Import {ArchiveJobId} completed for {UserId}", batch.ArchiveJobId, batch.UserId);
        }
        else
        {
            logger.LogInformation("Import {ArchiveJobId} not complete yet for {UserId}", batch.ArchiveJobId, batch.UserId);
        }

        await RaiseEventsAsync(import);

        await dbContext.AddRangeAsync(pinsToCreate, cancellationToken);

        var pinsIdsToAssignTags = updatedPins.Select(x => x.Id).ToList().Concat(pinsToCreate.Select(x => x.Id));
        foreach (Guid pinId in pinsIdsToAssignTags)
        {
            await bus.PublishAsync(new AssignTagsToPinMessage(pinId));
        }

        return;

        Pin CreatePin(GooglePlaceId placeId, StarredPlace place)
        {
            return new Pin(batch.UserId, place.Name!, placeId, new Address(place.Address!, place.CountryCode!.Value),
                new Location(place.Latitude!.Value, place.Longitude!.Value), place.AddedDate);
        }
    }

    private async Task<IImportService> CreateClientAsync(string userId)
    {
        var result = await factory.CreateAsync(userId);
        if (result.IsFailed)
        {
            logger.LogError("Failed to create import service: {Errors}", result.Errors);
            throw new InvalidOperationException("Failed to create import service");
        }

        return result.Value;
    }

    private async Task<Import?> GetCurrentImport(ICorrelatedMessage message, CancellationToken cancellationToken)
    {
        return await dbContext.Imports
            .FirstOrDefaultAsync(
                x => x.UserId == message.UserId && x.ArchiveJobId == message.ArchiveJobId && x.State == ImportState.InProgress,
                cancellationToken);
    }
}