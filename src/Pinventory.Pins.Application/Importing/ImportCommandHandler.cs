using FluentResults;

using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Application.Results;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Infrastructure.Sagas;

using Wolverine.Persistence;

namespace Pinventory.Pins.Application.Importing;

public sealed class ImportCommandHandler(
    ILogger<ImportCommandHandler> logger,
    IImportServiceFactory factory,
    IImportConcurrencyPolicy concurrencyPolicy)
{
    public async Task<(ResultDto<Guid> Result, CheckJobMessage? Message, IStorageAction<Import> Storage)> HandleAsync(
        StartImportCommand command,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting import for {UserId}", command.UserId);
        var periodResult = Period.Create(command.Start, command.End);
        if (periodResult.IsFailed)
        {
            return (Result.Fail<Guid>(periodResult.Errors).ToResultDto(), null, Storage.Nothing<Import>());
        }

        var clientResult = await factory.CreateAsync(command.UserId, cancellationToken);
        if (clientResult.IsFailed)
        {
            return (Result.Fail<Guid>(clientResult.Errors).ToResultDto(), null, Storage.Nothing<Import>());
        }

        var client = clientResult.Value;

        string archiveJobId;

        var archiveJobIdResult = await client.InitiateAsync(cancellationToken: cancellationToken);
        if (archiveJobIdResult.IsSuccess)
        {
            archiveJobId = archiveJobIdResult.Value;
        }
        // If an archive job already exists for the user, use the existing job
        else if (archiveJobIdResult.HasError<Errors.ImportHandler.ArchiveJobExists>(out var errors)
                 && errors.First().ArchiveJobId is { } extractedArchiveJobId)
        {
            archiveJobId = extractedArchiveJobId;
        }
        else
        {
            logger.LogError("Failed to initiate archive job: {Errors}", archiveJobIdResult.Errors);
            return (Result.Fail<Guid>(archiveJobIdResult.Errors).ToResultDto(), null, Storage.Nothing<Import>());
        }

        var import = new Import(command.UserId, periodResult.Value);
        if (await import.StartAsync(archiveJobId, concurrencyPolicy) is { IsFailed: true } result)
        {
            return (Result.Fail<Guid>(result.Errors).ToResultDto(), null, Storage.Nothing<Import>());
        }

        return (Result.Ok(import.Id).ToResultDto(),
            new CheckJobMessage(import.Id, import.UserId, import.ArchiveJobId!),
            Storage.Insert(import));
    }

    public (ResultDto Result, CheckJobMessage? Message, ImportStarted? Event) Handle(
        RenewImportCommand command,
        Import? import,
        ImportProcess? saga)
    {
        logger.LogInformation("Renewing import '{ImportId}' for {UserId}", command.ImportId, command.UserId);
        if (import is null)
        {
            var error = Errors.ImportHandler.RunningImportNotFound(command.UserId, command.ImportId);
            return (Result.Fail(error).ToResultDto(), null, null);
        }

        if (import.ClearPlaces() is { IsFailed: true } result)
        {
            return (Result.Fail(result.Errors).ToResultDto(), null, null);
        }

        // Check if saga exists, if not, create it by event
        ImportStarted? @event = null;
        if (saga is null)
        {
            logger.LogWarning("Import process [Saga] not found for {ImportId}", import.Id);
            @event = new ImportStarted(import.Id, import.UserId, import.ArchiveJobId!);
        }

        return (ResultDto.Ok(), new CheckJobMessage(import.Id, import.UserId, import.ArchiveJobId!), @event);
    }

    public async Task<ResultDto> HandleAsync(
        CancelImportCommand command,
        Import? import,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Cancelling import '{ImportId}' for {UserId}", command.ImportId, command.UserId);
        if (import is null)
        {
            return Result.Fail(Errors.ImportHandler.RunningImportNotFound(command.UserId, command.ImportId)).ToResultDto();
        }

        var clientResult = await factory.CreateAsync(command.UserId, cancellationToken);
        if (clientResult is { IsFailed: true })
        {
            return Result.Fail<string>(clientResult.Errors).ToResultDto();
        }

        var client = clientResult.Value;
        if (await client.CancelJobAsync(import.ArchiveJobId!, cancellationToken) is { IsFailed: true } cancelResult)
        {
            var failResult = import.Fail(cancelResult.Errors[0]);
            if (failResult.IsFailed)
            {
                logger.LogError("Failed to fail import job: {Errors}", failResult.Errors);
            }

            var errors = cancelResult.Errors.Concat(failResult.Errors);
            return Result.Fail(errors).ToResultDto();
        }

        if (import.Cancel() is { IsFailed: true } result)
        {
            logger.LogError("Failed to cancel import job: {Errors}", result.Errors);
            return Result.Fail(result.Errors).ToResultDto();
        }

        return ResultDto.Ok();
    }
}