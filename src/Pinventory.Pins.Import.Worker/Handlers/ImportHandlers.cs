using FluentResults;

using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Importing.Messages;

using Wolverine.Attributes;

namespace Pinventory.Pins.Import.Worker.Handlers;

[WolverineHandler]
public static class ImportHandlers
{
    public static async Task<Result<string>> HandleAsync(StartImportCommand command, ImportHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(command, cancellationToken);

    public static async Task<Result<Success>> HandleAsync(CancelImportCommand command, ImportHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(command, cancellationToken);

    public static async Task HandleAsync(CheckJobMessage check, ImportHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(check, cancellationToken);

    public static async Task HandleAsync(DownloadArchiveMessage download, ImportHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(download, cancellationToken);

    public static async Task HandleAsync(ProcessPinsBatchMessage batch, ImportHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(batch, cancellationToken);
}