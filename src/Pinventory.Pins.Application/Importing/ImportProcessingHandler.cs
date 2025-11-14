using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Tagging.Messages;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Infrastructure;

using Wolverine;

namespace Pinventory.Pins.Application.Importing;

// dbContext.SaveChangesAsync() is not used because Wolverine handles transactional outbox 
public class ImportProcessingHandler(
    ILogger<ImportProcessingHandler> logger,
    PinsDbContext dbContext,
    IMessageContext bus,
    IStaredPlaceValidator validator) : ApplicationHandler(bus)
{
    public async Task HandleAsync(ImportBatchRegistered batch, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Import {ArchiveJobId}: Processing batch {BatchId} pins for user {UserId}", batch.ArchiveJobId,
            batch.BatchId, batch.UserId);

        var import = await dbContext.GetCurrentImport(batch.AggregateId, cancellationToken);
        if (import is null)
        {
            logger.LogError("Running import {ArchiveJobId} not found for {UserId}", batch.ArchiveJobId, batch.UserId);
            return;
        }

        var processResult = await import.ProcessBatchAsync(batch.BatchId, validator, cancellationToken);
        if (processResult.IsFailed)
        {
            logger.LogError("Processing batch with id {BatchId} in job {ArchiveJobId} failed for user {UserId}", batch.BatchId,
                import.ArchiveJobId, import.UserId);
            return;
        }

        var (toCreate, toUpdate) = processResult.Value;

        var newPins = toCreate.Select(x => Pin.Create(import.UserId, GooglePlaceId.Parse(x.GoogleMapsUrl), x)).ToList();
        await dbContext.Pins.AddRangeAsync(newPins, cancellationToken);

        var placesToUpdate = toUpdate.ToDictionary(x => GooglePlaceId.Parse(x.GoogleMapsUrl));
        var pinsToUpdate = dbContext.Pins.Where(x => x.OwnerId == import.UserId && placesToUpdate.Keys.Contains(x.PlaceId))
            .AsAsyncEnumerable().WithCancellation(cancellationToken);

        List<Guid> updatedPinIds = [];
        await foreach (var pin in pinsToUpdate)
        {
            updatedPinIds.Add(pin.Id);
            var name = placesToUpdate[pin.PlaceId].Name;
            if (name != null)
            {
                pin.Rename(name);
            }
        }

        await RaiseEventsAsync(import);

        foreach (Guid pinId in GetChangedPinIds())
        {
            await bus.PublishAsync(new AssignTagsToPinMessage(pinId));
        }

        return;

        IEnumerable<Guid> GetChangedPinIds() =>
            updatedPinIds.Concat(newPins.Select(x => x.Id)).ToList();
    }

    public async Task HandleAsync(ImportBatchProcessed processed, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Import {ArchiveJobId}: try complete process for user {UserId}", processed.ArchiveJobId, processed.UserId);

        var import = await dbContext.GetCurrentImport(processed.AggregateId, cancellationToken);
        if (import is null)
        {
            logger.LogError("Running import {ArchiveJobId} not found for {UserId}", processed.ArchiveJobId, processed.UserId);
            return;
        }

        var tryComplete = import.TryComplete();
        if (tryComplete.IsFailed)
        {
            logger.LogError("Failed to complete import job: {Errors}", tryComplete.Errors);
            return;
        }

        if (tryComplete.Value)
        {
            logger.LogInformation("Import {ArchiveJobId} completed for {UserId}", processed.ArchiveJobId, processed.UserId);
        }
        else
        {
            logger.LogInformation("Import {ArchiveJobId} not complete yet for {UserId}", processed.ArchiveJobId, processed.UserId);
        }

        await RaiseEventsAsync(import);
    }
}