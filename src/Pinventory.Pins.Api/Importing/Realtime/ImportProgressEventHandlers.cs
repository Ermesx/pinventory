using Microsoft.AspNetCore.SignalR;

using Pinventory.Pins.Api.Importing.Dtos;
using Pinventory.Pins.Domain.Importing.Events;

using Wolverine.Attributes;

namespace Pinventory.Pins.Api.Importing.Realtime;

[WolverineHandler]
public static class ImportProgressEventHandlers
{
    public static Task Handle(ImportBatchProcessed @event, IHubContext<ImportProgressHub, IImportProgressClient> hub)
        => hub.Clients.Group(ImportProgressHub.UserGroup(@event.UserId))
            .ProgressUpdated(new ImportProgressDto(@event.AggregateId, @event.ArchiveJobId,
                @event.Processed, @event.Created,
                @event.Updated, @event.Failed,
                @event.Conflicts, @event.Total));

    public static Task Handle(ImportCompleted @event, IHubContext<ImportProgressHub, IImportProgressClient> hub)
        => hub.Clients.Group(ImportProgressHub.UserGroup(@event.UserId))
            .ImportCompleted(new ImportCompletedDto(@event.AggregateId, @event.ArchiveJobId));

    public static Task Handle(ImportFailed @event, IHubContext<ImportProgressHub, IImportProgressClient> hub)
        => hub.Clients.Group(ImportProgressHub.UserGroup(@event.UserId))
            .ImportFailed(new ImportFailedDto(@event.AggregateId, @event.ArchiveJobId, @event.Error));
}