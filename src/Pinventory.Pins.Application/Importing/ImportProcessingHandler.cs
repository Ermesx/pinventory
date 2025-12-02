using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Tagging.Messages;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Infrastructure;
using Pinventory.Pins.Infrastructure.Sagas.Messages;

using Wolverine;

namespace Pinventory.Pins.Application.Importing;

// dbContext.SaveChangesAsync() is not used because Wolverine handles transactional outbox 
public class ImportProcessingHandler(
    ILogger<ImportProcessingHandler> logger,
    PinsDbContext dbContext,
    IMessageContext bus,
    IStarredPlaceValidator validator) : ApplicationHandler(bus)
{
    public const int MaxBatchSize = 300;

    public async Task<OutgoingMessages> HandleAsync(PlacesProcessingBatchMessage batch, CancellationToken cancellationToken = default)
    {
        var outgoingMessages = new OutgoingMessages();

        logger.LogInformation("Import {ArchiveJobId}: Processing places into pins for user {UserId}", batch.ArchiveJobId,
            batch.UserId);
        if (await dbContext.GetCurrentImport(batch.ImportId, cancellationToken) is not { } import)
        {
            logger.LogError("Running import {ArchiveJobId} not found for {UserId}", batch.ArchiveJobId, batch.UserId);
            return outgoingMessages;
        }

        var placesIds = batch.PlaceIds.ToHashSet();
        var processResult = await import.ProcessPlacesAsync(placesIds, validator, cancellationToken);
        if (processResult.IsFailed)
        {
            logger.LogError("Processing places in job {ArchiveJobId} failed for user {UserId}", batch.ArchiveJobId,
                batch.UserId);
            return outgoingMessages;
        }

        var (toCreate, toUpdate) = processResult.Value;

        var newPins = await CreatePins(toCreate);
        var updatedPins = await UpdatePins(toUpdate);

        await RaiseEventsAsync(import);

        outgoingMessages.AddRange(GetIdsForChangedPins().Select(pinId => new AssignTagsToPinMessage(pinId)));
        outgoingMessages.Add(new BatchCompletedMessage(batch.ImportId, batch.UserId, batch.ArchiveJobId));

        return outgoingMessages;

        IEnumerable<Guid> GetIdsForChangedPins() => updatedPins.Select(x => x.Id).Concat(newPins.Select(x => x.Id)).ToList();

        async Task<List<Pin>> CreatePins(IEnumerable<StarredPlace> create)
        {
            var pinsToCreate = create.Select(x => Pin.Create(import.UserId, GooglePlaceId.Parse(x.GoogleMapsUrl), x)).ToList();
            await dbContext.Pins.AddRangeAsync(pinsToCreate, cancellationToken);
            return pinsToCreate;
        }

        async Task<List<Pin>> UpdatePins(IEnumerable<StarredPlace> update)
        {
            var placesToUpdate = update.ToDictionary(x => GooglePlaceId.Parse(x.GoogleMapsUrl));

            var allUserPins = await dbContext.Pins
                .Where(x => x.OwnerId == import.UserId)
                .ToListAsync(cancellationToken);

            var pinsToUpdate = allUserPins
                .Where(x => placesToUpdate.ContainsKey(x.PlaceId))
                .ToList();

            foreach (var pin in pinsToUpdate)
            {
                var name = placesToUpdate[pin.PlaceId].Name;
                if (name != null)
                {
                    pin.Rename(name);
                }
            }

            return pinsToUpdate;
        }
    }

    public async Task HandleAsync(ImportProcessCompleted completed, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Import {ArchiveJobId}: try complete process for user {UserId}", completed.ArchiveJobId, completed.UserId);

        var import = await dbContext.GetCurrentImport(completed.ImportId, cancellationToken);
        if (import is null)
        {
            logger.LogError("Running import {ArchiveJobId} not found for {UserId}", completed.ArchiveJobId, completed.UserId);
            return;
        }

        var complete = import.Complete();
        if (complete.IsFailed)
        {
            logger.LogError("Failed to complete import job: {Errors}", complete.Errors);
            import.Fail(complete.Errors[0]);
            return;
        }

        logger.LogInformation("Import {ArchiveJobId} completed for {UserId}", completed.ArchiveJobId, completed.UserId);
        await RaiseEventsAsync(import);
    }
}