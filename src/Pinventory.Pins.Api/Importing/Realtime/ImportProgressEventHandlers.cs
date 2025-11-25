using Microsoft.AspNetCore.SignalR;

using Pinventory.Pins.Api.Importing.Dtos;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;

using Wolverine.Attributes;

namespace Pinventory.Pins.Api.Importing.Realtime;

[WolverineHandler]
public static class ImportProgressEventHandlers
{
    public static Task Handle(ImportPlaceProcessed[] events, IHubContext<ImportProgressHub, IImportProgressClient> hub)
    {
        var (importId, userId, archiveJobId) = events.GetIdentifiers();
        var counters = events.CountBy(e => e.PlaceState).ToDictionary();
        return hub.Clients.Group(ImportProgressHub.UserGroup(userId))
            .ProgressUpdated(new ImportProgressDto(importId, archiveJobId,
                events.Length,
                counters[StarredPlaceState.New],
                counters[StarredPlaceState.Exists],
                counters[StarredPlaceState.Invalid],
                counters[StarredPlaceState.Conflicted]));
    }

    public static Task Handle(ImportCompleted @event, IHubContext<ImportProgressHub, IImportProgressClient> hub)
        => hub.Clients.Group(ImportProgressHub.UserGroup(@event.UserId))
            .ImportCompleted(new ImportCompletedDto(@event.Id, @event.ArchiveJobId));

    public static Task Handle(ImportFailed @event, IHubContext<ImportProgressHub, IImportProgressClient> hub)
        => hub.Clients.Group(ImportProgressHub.UserGroup(@event.UserId))
            .ImportFailed(new ImportFailedDto(@event.Id, @event.ArchiveJobId, @event.Error));
}