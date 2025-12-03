using Pinventory.Pins.Application;
using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Results;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Infrastructure.Sagas;
using Pinventory.Pins.Infrastructure.Sagas.Messages;

using Wolverine;
using Wolverine.Attributes;
using Wolverine.Persistence;
using Wolverine.RabbitMQ;

namespace Pinventory.Pins.Import.Worker.Handlers;

[WolverineHandler]
public static class ImportHandlers
{
    public static async Task<(ResultDto<Guid>, CheckJobMessage?, IStorageAction<Domain.Importing.Import>)> HandleAsync(
        StartImportCommand command,
        ImportCommandHandler app, CancellationToken cancellationToken = default) =>
        await app.HandleAsync(command, cancellationToken);

    public static (ResultDto, CheckJobMessage?, ImportStarted?) Handle(RenewImportCommand command,
        Domain.Importing.Import? import,
        [Entity(Required = false)] ImportProcess? saga,
        ImportCommandHandler app) =>
        app.Handle(command, import, saga);

    public static async Task<ResultDto> HandleAsync(
        CancelImportCommand command,
        Domain.Importing.Import? import,
        ImportCommandHandler app, CancellationToken cancellationToken = default) =>
        await app.HandleAsync(command, import, cancellationToken);

    public static async Task<(DownloadArchiveMessage?, CheckJobMessage?)> HandleAsync(CheckJobMessage check,
        Domain.Importing.Import import,
        ImportDownloadHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(check, import, cancellationToken);

    public static async Task<ExpectedBatchesMessage?> HandleAsync(DownloadArchiveMessage download,
        Domain.Importing.Import import,
        ImportDownloadHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(download, import, cancellationToken);

    public static async Task<OutgoingMessages> HandleAsync(PlacesProcessingBatchMessage batchMessage,
        Domain.Importing.Import? import,
        ImportProcessingHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(batchMessage, import, cancellationToken);

    public static void Handle(ImportProcessCompleted completed,
        Domain.Importing.Import? import,
        ImportProcessingHandler app,
        CancellationToken cancellationToken = default) =>
        app.Handle(completed, import);

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