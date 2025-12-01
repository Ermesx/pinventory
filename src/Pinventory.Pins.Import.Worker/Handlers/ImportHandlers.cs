using Pinventory.Pins.Application;
using Pinventory.Pins.Application.Abstractions.Results;
using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Infrastructure.Sagas.Messages;

using Wolverine;
using Wolverine.Attributes;
using Wolverine.RabbitMQ;

namespace Pinventory.Pins.Import.Worker.Handlers;

[WolverineHandler]
public static class ImportHandlers
{
    public static async Task<ResultDto<string>> HandleAsync(StartImportCommand command, ImportCommandHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(command, cancellationToken);

    // Add => [Entity(Required = false)] ImportProcess? saga
    public static async Task<ResultDto> HandleAsync(RenewImportCommand command,
        ImportCommandHandler app,
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

    public static async Task HandleAsync(PlacesProcessingBatchMessage batchMessage, ImportProcessingHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(batchMessage, cancellationToken);

    public static async Task HandleAsync(ImportProcessCompleted completed, ImportProcessingHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(completed, cancellationToken);

    public static void RouteImportProcessing(this WolverineOptions options)
    {
        // Commands
        options.ListenToRabbitQueue(PinsMessaging.QueueNames.ImportCommands);

        // Download
        options.PublishMessage<CheckJobMessage>().Locally();
        options.PublishMessage<DownloadArchiveMessage>().Locally();

        // Saga
        options.PublishMessage<ExpectedBatchesMessage>().Locally();
        options.PublishMessage<BatchCompletedMessage>().Locally();
        options.PublishMessage<ImportProcessCompleted>().Locally();
        options.PublishMessage<ImportProcessTimeout>().Locally();
    }
}