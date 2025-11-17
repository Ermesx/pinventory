using FluentResults;

using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Abstractions.Results;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure;

using Wolverine;

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
        logger.LogInformation("Starting importMessage for {UserId}", command.UserId);
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
        var archiveJobIdResult = await client.InitiateAsync(cancellationToken: cancellationToken);
        if (archiveJobIdResult.IsFailed)
        {
            if (archiveJobIdResult.HasError<Errors.Import.ArchiveJobExists>())
            {
                var currentImport = await dbContext.GetCurrentImport(command.UserId, cancellationToken);
                if (currentImport is not null)
                {
                    await bus.ScheduleAsync(new CheckJobMessage(currentImport.Id, currentImport.UserId, currentImport.ArchiveJobId!),
                        CheckJobMessage.CheckInterval);
                    return Result.Ok(currentImport.ArchiveJobId!).ToResultDto();
                }
            }

            logger.LogError("Failed to initiate archive job: {Errors}", archiveJobIdResult.Errors);
            return Result.Fail<string>(archiveJobIdResult.Errors).ToResultDto();
        }

        var archiveJobId = archiveJobIdResult.Value;

        var import = new Import(command.UserId, periodResult.Value);
        var result = await import.StartAsync(archiveJobId, concurrencyPolicy);

        if (result.IsFailed)
        {
            return Result.Fail<string>(result.Errors).ToResultDto();
        }

        await dbContext.Imports.AddAsync(import, cancellationToken);
        await RaiseEventsAsync(import);

        await bus.ScheduleAsync(new CheckJobMessage(import.Id, import.UserId, archiveJobId), CheckJobMessage.CheckInterval);

        return Result.Ok(archiveJobId).ToResultDto();
    }

    public async Task<ResultDto> HandleAsync(CancelImportCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Cancelling import '{ArchiveJobId}' for {UserId}", command.ArchiveJobId, command.UserId);

        var import = await dbContext.GetCurrentImport(command.UserId, cancellationToken);
        if (import is null)
        {
            return Result.Fail(Errors.Import.RunningImportNotFound(command.UserId, command.ArchiveJobId)).ToResultDto();
        }

        var clientResult = await factory.CreateAsync(command.UserId, cancellationToken);
        if (clientResult.IsFailed)
        {
            return Result.Fail<string>(clientResult.Errors).ToResultDto();
        }

        var client = clientResult.Value;
        var cancelResult = await client.CancelJobAsync(command.ArchiveJobId, cancellationToken);
        if (cancelResult.IsFailed)
        {
            var failResult = import.Fail(cancelResult.Errors[0]);
            if (failResult.IsFailed)
            {
                logger.LogError("Failed to fail import job: {Errors}", failResult.Errors);
            }

            var errors = cancelResult.Errors.Concat(failResult.Errors);
            return Result.Fail(errors).ToResultDto();
        }

        var result = import.Cancel();
        if (result.IsFailed)
        {
            logger.LogError("Failed to cancel import job: {Errors}", result.Errors);
            return Result.Fail(result.Errors).ToResultDto();
        }

        await RaiseEventsAsync(import);
        return ResultDto.Ok();
    }
}