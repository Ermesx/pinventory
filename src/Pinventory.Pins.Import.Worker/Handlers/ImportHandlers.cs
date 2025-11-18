using Pinventory.Pins.Application.Abstractions.Results;
using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Infrastructure.Sagas.Messages;

using Wolverine.Attributes;

namespace Pinventory.Pins.Import.Worker.Handlers;

[WolverineHandler]
public static class ImportHandlers
{
    public static async Task<ResultDto<string>> HandleAsync(StartImportCommand command, ImportCommandHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(command, cancellationToken);

    public static async Task<ResultDto> HandleAsync(CancelImportCommand command, ImportCommandHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(command, cancellationToken);

    public static async Task HandleAsync(CheckJobMessage check, ImportDownloadHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(check, cancellationToken);

    public static async Task HandleAsync(DownloadArchiveMessage download, ImportDownloadHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(download, cancellationToken);

    public static async Task HandleAsync(ImportBatchRegistered batch, ImportProcessingHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(batch, cancellationToken);

    public static async Task HandleAsync(ImportProcessCompleted completed, ImportProcessingHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(completed, cancellationToken);
}