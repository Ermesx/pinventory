using JasperFx.Core.Reflection;

using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Domain.Importing.Events;

using Wolverine;
using Wolverine.Runtime.Batching;

namespace Pinventory.Pins.Application.Importing.Services;

public class ImportProcessingBatcher : IMessageBatcher
{
    public IEnumerable<Envelope> Group(IReadOnlyList<Envelope> envelopes)
    {
        var groups = envelopes
            .GroupBy(x => x.Message!.As<ImportPlaceRegistered>().Id)
            .ToArray();

        foreach (var group in groups)
        {
            var places = group
                .Select(x => x.Message)
                .OfType<ImportPlaceRegistered>().ToList();

            var placesIds = places.Select(x => x.PlaceId).ToArray();
            var (_, userId, archiveJobId) = places.GetIdentifiers();

            var message = new PlacesProcessingBatch(group.Key, userId, archiveJobId, placesIds);

            yield return new Envelope(message, group);
        }
    }

    public Type BatchMessageType => typeof(PlacesProcessingBatch);
}