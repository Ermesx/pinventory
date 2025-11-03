using FluentResults;

using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Import.Commands;
using Pinventory.Pins.Application.Import.Messages;
using Pinventory.Pins.Application.Import.Services;
using Pinventory.Pins.Domain.Import;
using Pinventory.Pins.Infrastructure;

using Wolverine;

namespace Pinventory.Pins.Application.Import;

public sealed class ImportHandler(
    ILogger<ImportHandler> logger,
    IImportServiceFactory factory,
    PinsDbContext dbContext,
    IMessageContext bus,
    IImportConcurrencyPolicy concurrencyPolicy) : ApplicationHandler(bus)
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    public async Task<Result<string>> HandleAsync(StartImportCommand command)
    {
        logger.LogInformation("Starting importMessage for {UserId}", command.UserId);

        var client = await CreateClientAsync(command.UserId);
        var archiveJobId = await client.InitiateAsync(command.Period);

        var importJob = new ImportJob(command.UserId);
        var result = await importJob.StartAsync(archiveJobId, concurrencyPolicy);

        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        await dbContext.ImportJobs.AddAsync(importJob);
        await RaiseEventsAsync(importJob);

        await bus.PublishAsync(new CheckJobMessage(importJob.UserId, importJob.ArchiveJobId!));

        return Result.Ok(archiveJobId);
    }

    public async Task HandleAsync(CheckJobMessage check)
    {
        logger.LogInformation("Checking archive {ArchiveJobId}", check.ArchiveJobId);

        var client = await CreateClientAsync(check.UserId);
        var archiveResult = await client.CheckJobAsync(check.ArchiveJobId);

        if (archiveResult.State == ImportJobState.InProgress)
        {
            logger.LogInformation("Archive {ArchiveJobId} is still in progress", check.ArchiveJobId);
            await bus.ReScheduleCurrentAsync(DateTimeOffset.UtcNow.Add(CheckInterval));
            return;
        }

        foreach (var url in archiveResult.Urls)
        {
            var download = new DownloadArchiveMessage(url.ToString());
            await bus.PublishAsync(download);
        }
    }

    private async Task<IImportService> CreateClientAsync(string userId)
    {
        var result = await factory.CreateAsync(userId);
        if (result.IsFailed)
        {
            logger.LogError("Failed to create import service: {Errors}", result.Errors);
            throw new InvalidOperationException("Failed to create import service");
        }

        return result.Value;
    }
}