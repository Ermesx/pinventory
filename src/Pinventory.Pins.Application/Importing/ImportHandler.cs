using FluentResults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Abstractions.Results;
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

// dbContext.SaveChangesAsync() is not used because Wolverine handles transactional outbox 
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

    public async Task<ResultDto<string>> HandleAsync(StartImportCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting importMessage for {UserId}", command.UserId);
        var periodResult = Period.Create(command.Start, command.End);
        if (periodResult.IsFailed)
        {
            return Result.Fail<string>(periodResult.Errors).ToResultDto();
        }

        var client = await CreateClientAsync(command.UserId);
        var archiveJobIdResult = await client.InitiateAsync(cancellationToken: cancellationToken);
        if (archiveJobIdResult.IsFailed)
        {
            if (archiveJobIdResult.HasError<Errors.Import.ArchiveJobExists>())
            {
                var currentImport = await GetCurrentImport(command.UserId, cancellationToken);
                if (currentImport is not null)
                {
                    await bus.PublishAsync(new CheckJobMessage(currentImport.UserId, currentImport.ArchiveJobId!));
                    return Result.Ok(currentImport.ArchiveJobId!).ToResultDto();
                }
            }

            logger.LogError("Failed to initiate archive job: {Errors}", archiveJobIdResult.Errors);
            return Result.Fail<string>(archiveJobIdResult.Errors).ToResultDto();
        }

        var archiveJobId = archiveJobIdResult.Value;

        var import = new Import(command.UserId, periodResult.Value);
        var result = await import.StartAsync(archiveJobId, concurrencyPolicy);

        if (result.IsFailed)
        {
            return Result.Fail<string>(result.Errors).ToResultDto();
        }

        await dbContext.Imports.AddAsync(import, cancellationToken);
        await RaiseEventsAsync(import);

        await bus.PublishAsync(new CheckJobMessage(import.UserId, archiveJobId));

        return Result.Ok(archiveJobId).ToResultDto();
    }

    public async Task<ResultDto> HandleAsync(CancelImportCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Cancelling import '{ArchiveJobId}' for {UserId}", command.ArchiveJobId, command.UserId);

        var import = await GetCurrentImport(command, cancellationToken);
        if (import is null)
        {
            return Result.Fail(Errors.Import.RunningImportNotFound(command)).ToResultDto();
        }

        var client = await CreateClientAsync(command.UserId);
        var cancelResult = await client.CancelJobAsync(command.ArchiveJobId, cancellationToken);
        if (cancelResult.IsFailed)
        {
            var failResult = import.Fail(cancelResult.Errors[0]);
            if (failResult.IsFailed)
            {
                logger.LogError("Failed to fail import job: {Errors}", failResult.Errors);
            }

            var errors = cancelResult.Errors.Concat(failResult.Errors);
            return Result.Fail(errors).ToResultDto();
        }

        var result = import.Cancel();
        if (result.IsFailed)
        {
            logger.LogError("Failed to cancel import job: {Errors}", result.Errors);
            return Result.Fail(result.Errors).ToResultDto();
        }

        await RaiseEventsAsync(import);
        return ResultDto.Ok();
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
        if (archiveResult.IsFailed)
        {
            logger.LogError("Failed to check archive job: {Errors}", archiveResult.Errors);
            return;
        }

        Result<Success> result;
        switch (archiveResult.Value.State)
        {
            case ImportState.InProgress:
                logger.LogInformation("Archive {ArchiveJobId} is still in progress for {UserId}", check.ArchiveJobId, check.UserId);
                await bus.ReScheduleCurrentAsync(DateTimeOffset.UtcNow.Add(CheckJobMessage.CheckInterval));
                return;
            case ImportState.Failed:
                logger.LogWarning("Archive {ArchiveJobId} failed for {UserId}", check.ArchiveJobId, check.UserId);
                result = import.Fail(new Error("Archive job failed externally"));
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
                var urls = archiveResult.Value.Urls.Select(x => x.ToString()).ToList();
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

        var dataFilesUri = new Uri(download.Urls[0]);
        var archiveBrowserUri = new Uri(download.Urls[1]);

        var result = await downloader.DownloadAsync(archiveBrowserUri, dataFilesUri, cancellationToken);
        if (result.IsFailed)
        {
            logger.LogError("Failed to download archive: {Errors}", result.Errors);
            return;
        }

        var records = result.Value.Data.Features;
        import.SetTotal((uint)records.Length);

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
                place.Geometry.Coordinates[1],
                place.Geometry.Coordinates[0],
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

        var existingPins = await dbContext.Pins.Where(x => x.OwnerId == batch.UserId).ToListAsync(cancellationToken: cancellationToken);
        foreach (var place in batch.StarredPlaces)
        {
            processed++;

            if (!IsValidPlace(place) || !GooglePlaceId.TryParse(place.GoogleMapsUrl, out var placeId))
            {
                failedPlaces.Add(new ReportedPlace(place.GoogleMapsUrl, place.AddedDate));
                failed++;
                continue;
            }

            // Conflicted places are those with the same name but different place ID
            var possibleConflictedPin = existingPins.FirstOrDefault(x => x.Name == place.Name && x.PlaceId != placeId);
            if (possibleConflictedPin is not null)
            {
                conflictingPlaces.Add(new ReportedPlace(place.GoogleMapsUrl, place.AddedDate));
                conflicts++;
                continue;
            }

            var existingPin = existingPins.SingleOrDefault(x => x.PlaceId == placeId);
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

        LogCompletion(tryComplete.Value);

        await RaiseEventsAsync(import);

        await dbContext.AddRangeAsync(pinsToCreate, cancellationToken);

        foreach (Guid pinId in GetChangedPinIds())
        {
            await bus.PublishAsync(new AssignTagsToPinMessage(pinId));
        }

        return;

        static bool IsValidPlace(StarredPlace place) =>
            place is { Name: not null, Address: not null, Comment: not RemovedPlaceComment };

        Pin CreatePin(GooglePlaceId placeId, StarredPlace place)
        {
            return new Pin(batch.UserId, place.Name!, placeId, new Address(place.Address!, place.CountryCode!.Value),
                new Location(place.Latitude!.Value, place.Longitude!.Value), place.AddedDate);
        }

        IEnumerable<Guid> GetChangedPinIds() =>
            updatedPins.Select(x => x.Id).Concat(pinsToCreate.Select(x => x.Id)).ToList();

        void LogCompletion(bool completed)
        {
            if (completed)
            {
                logger.LogInformation("Import {ArchiveJobId} completed for {UserId}", batch.ArchiveJobId, batch.UserId);
            }
            else
            {
                logger.LogInformation("Import {ArchiveJobId} not complete yet for {UserId}", batch.ArchiveJobId, batch.UserId);
            }
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

    private async Task<Import?> GetCurrentImport(string userId, CancellationToken cancellationToken)
        => await dbContext.Imports
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.State == ImportState.InProgress, cancellationToken);

    private async Task<Import?> GetCurrentImport(ICorrelatedMessage message, CancellationToken cancellationToken)
        => await dbContext.Imports
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                x => x.UserId == message.UserId && x.ArchiveJobId == message.ArchiveJobId && x.State == ImportState.InProgress,
                cancellationToken);
}