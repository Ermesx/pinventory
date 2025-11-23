using FluentResults;

using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Abstractions.Results;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Infrastructure;
using Pinventory.Pins.Infrastructure.Sagas;

using Wolverine;
using Wolverine.Persistence;

namespace Pinventory.Pins.Application.Importing;

// dbContext.SaveChangesAsync() is not used because Wolverine handles transactional outbox 
public sealed class ImportCommandHandler(
    ILogger<ImportCommandHandler> logger,
    IImportServiceFactory factory,
    PinsDbContext dbContext,
    IMessageContext bus,
    IImportConcurrencyPolicy concurrencyPolicy) : ApplicationHandler(bus)
{
    public async Task<ResultDto<string>> HandleAsync(StartImportCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting import for {UserId}", command.UserId);
        var periodResult = Period.Create(command.Start, command.End);
        if (periodResult.IsFailed)
        {
            return Result.Fail<string>(periodResult.Errors).ToResultDto();
        }

        var clientResult = await factory.CreateAsync(command.UserId, cancellationToken);
        if (clientResult.IsFailed)
        {
            return Result.Fail<string>(clientResult.Errors).ToResultDto();
        }

        var client = clientResult.Value;

        string archiveJobId;

        var archiveJobIdResult = await client.InitiateAsync(cancellationToken: cancellationToken);
        if (archiveJobIdResult.IsSuccess)
        {
            archiveJobId = archiveJobIdResult.Value;
        }
        // If an archive job already exists and there is no running import, use the existing job
        else if (archiveJobIdResult.HasError<Errors.Import.ArchiveJobExists>(out var errors)
                 && await dbContext.GetCurrentImport(command.UserId, cancellationToken) is null
                 && errors.First().ArchiveJobId is { } extractedArchiveJobId)
        {
            archiveJobId = extractedArchiveJobId;
        }
        else
        {
            logger.LogError("Failed to initiate archive job: {Errors}", archiveJobIdResult.Errors);
            return Result.Fail<string>(archiveJobIdResult.Errors).ToResultDto();
        }

        var import = new Import(command.UserId, periodResult.Value);
        if (await import.StartAsync(archiveJobId, concurrencyPolicy) is { IsFailed: true } result)
        {
            return Result.Fail<string>(result.Errors).ToResultDto();
        }

        await dbContext.Imports.AddAsync(import, cancellationToken);
        await RaiseEventsAsync(import);

        await bus.ScheduleAsync(new CheckJobMessage(import.Id, import.UserId, archiveJobId), CheckJobMessage.CheckInterval);

        return Result.Ok(archiveJobId).ToResultDto();
    }

    public async Task<ResultDto> HandleAsync(RenewImportCommand command, [Entity(Required = false)] ImportProcess? saga,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Renewing import '{ArchiveJobId}' for {UserId}", command.ArchiveJobId, command.UserId);
        if (await dbContext.GetCurrentImport(command.UserId, cancellationToken) is not { } import)
        {
            return Result.Fail<string>(Errors.Import.RunningImportNotFound(command.UserId, command.ArchiveJobId)).ToResultDto();
        }

        if (import.ClearBatches() is { IsFailed: true } result)
        {
            return Result.Fail<string>(result.Errors).ToResultDto();
        }

        // Check if saga exists, if not, create it by event
        if (saga is null)
        {
            logger.LogError("Import process [Saga] not found for {ImportId}", import.Id);
            await bus.PublishAsync(new ImportStarted(import.Id, import.UserId, import.ArchiveJobId!));
        }

        await RaiseEventsAsync(import);

        await bus.ScheduleAsync(new CheckJobMessage(import.Id, import.UserId, import.ArchiveJobId!), CheckJobMessage.CheckInterval);

        return ResultDto.Ok();
    }


    public async Task<ResultDto> HandleAsync(CancelImportCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Cancelling import '{ArchiveJobId}' for {UserId}", command.ArchiveJobId, command.UserId);
        if (await dbContext.GetCurrentImport(command.UserId, cancellationToken) is not { } import)
        {
            return Result.Fail(Errors.Import.RunningImportNotFound(command.UserId, command.ArchiveJobId)).ToResultDto();
        }

        var clientResult = await factory.CreateAsync(command.UserId, cancellationToken);
        if (clientResult is { IsFailed: true })
        {
            return Result.Fail<string>(clientResult.Errors).ToResultDto();
        }

        var client = clientResult.Value;
        if (await client.CancelJobAsync(command.ArchiveJobId, cancellationToken) is { IsFailed: true } cancelResult)
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

        await RaiseEventsAsync(import);
        return ResultDto.Ok();
    }
}