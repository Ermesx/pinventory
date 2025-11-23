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
            .ProgressUpdated(new ImportProgressDto(@event.Id, @event.ArchiveJobId,
                @event.Processed, @event.Created,
                @event.Updated, @event.Failed,
                @event.Conflicts));

    public static Task Handle(ImportCompleted @event, IHubContext<ImportProgressHub, IImportProgressClient> hub)
        => hub.Clients.Group(ImportProgressHub.UserGroup(@event.UserId))
            .ImportCompleted(new ImportCompletedDto(@event.Id, @event.ArchiveJobId));

    public static Task Handle(ImportFailed @event, IHubContext<ImportProgressHub, IImportProgressClient> hub)
        => hub.Clients.Group(ImportProgressHub.UserGroup(@event.UserId))
            .ImportFailed(new ImportFailedDto(@event.Id, @event.ArchiveJobId, @event.Error));
}