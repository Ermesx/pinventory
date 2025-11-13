using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Tagging.Messages;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Infrastructure;

using Wolverine;

namespace Pinventory.Pins.Application.Importing;

// dbContext.SaveChangesAsync() is not used because Wolverine handles transactional outbox 
public class ImportProcessingHandler(
    ILogger<ImportProcessingHandler> logger,
    PinsDbContext dbContext,
    IMessageContext bus) : ApplicationHandler(bus)
{
    private const string RemovedPlaceComment = "No location information is available for this saved place";

    public async Task HandleAsync(ProcessPinsBatchMessage batch, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Import {ArchiveJobId}: Processing batch of {Count} pins for {UserId}", batch.ArchiveJobId,
            batch.StarredPlaces.Count(), batch.UserId);

        var import = await dbContext.GetCurrentImport(batch, cancellationToken);
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
}