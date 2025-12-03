using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    IStarredPlaceValidator validator)
{
    public const int MaxBatchSize = 300;

    // TODO: Consider this when move pins creations and updates as side effects

    // public async Task<IEnumerable<Pin>> LoadAsync(IUserMessage message, PinsDbContext dbContext,
    //     CancellationToken cancellationToken = default) =>
    //     await dbContext.Pins
    //         .Where(x => x.OwnerId == message.UserId)
    //         .ToListAsync(cancellationToken);


    public async Task<OutgoingMessages> HandleAsync(PlacesProcessingBatchMessage batch, Import? import,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Import {ImportId}: Processing places into pins for user {UserId}", batch.ImportId, batch.UserId);
        if (import is null)
        {
            logger.LogError("Running import {ImportId} not found for {UserId}", batch.ImportId, batch.UserId);
            return [];
        }

        var placesIds = batch.PlaceIds.ToHashSet();
        var processResult = await import.ProcessPlacesAsync(placesIds, validator, cancellationToken);
        if (processResult.IsFailed)
        {
            logger.LogError("Processing places in job {ImportId} failed for user {UserId}", batch.ImportId, batch.UserId);
            return [];
        }

        var (toCreate, toUpdate) = processResult.Value;

        Guid[] ids = [..CreatePins(toCreate), ..await UpdatePins(toUpdate)];

        var messages = ids.Select(pinId => new AssignTagsToPinMessage(pinId)).ToList();
        return [new BatchCompletedMessage(batch.ImportId, batch.UserId, batch.ArchiveJobId), ..messages];

        IEnumerable<Guid> CreatePins(IEnumerable<StarredPlace> create)
        {
            var pinsToCreate = create.Select(x => Pin.Create(import.UserId, GooglePlaceId.Parse(x.GoogleMapsUrl), x)).ToList();
            dbContext.Pins.AddRange(pinsToCreate);

            return pinsToCreate.Select(x => x.Id).ToList();
        }

        async Task<List<Guid>> UpdatePins(IReadOnlyList<StarredPlace> update)
        {
            if (!update.Any())
            {
                return [];
            }

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

            return pinsToUpdate.Select(x => x.Id).ToList();
        }
    }

    public void Handle(ImportProcessCompleted completed, Import? import)
    {
        logger.LogInformation("Import {ImportId}: try complete process for user {UserId}", completed.ImportId, completed.UserId);

        if (import is null)
        {
            logger.LogError("Running import {ImportId} not found for {UserId}", completed.ImportId, completed.UserId);
            return;
        }

        var complete = import.Complete();
        if (complete.IsFailed)
        {
            logger.LogError("Failed to complete import job: {Errors}", complete.Errors);
            import.Fail(complete.Errors[0]);
            return;
        }

        logger.LogInformation("Import {ImportId} completed for {UserId}", completed.ImportId, completed.UserId);
    }
}