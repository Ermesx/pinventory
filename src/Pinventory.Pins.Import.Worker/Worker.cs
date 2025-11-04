using Google;

using Pinventory.Pins.Application.Importing.Services;

namespace Pinventory.Pins.Import.Worker;

public class Worker(ILogger<Worker> logger, IImportServiceFactory importServiceFactory) : BackgroundService
{
    private string _name = string.Empty;
    private bool _oneTime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
            await TestRun(stoppingToken);
        }
    }

    private async Task TestRun(CancellationToken stoppingToken)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            var result = await importServiceFactory.CreateAsync("6aa93c5b-8e1f-433a-b52d-120282520103", stoppingToken);
            if (result.IsFailed)
            {
                logger.LogError("Failed to create import service: {Errors}", result.Errors);
                return;
            }

            var importService = result.Value;

            if (!_oneTime)
            {
                try
                {
                    var archiveName = await importService.InitiateAsync(null, stoppingToken);
                    _name = archiveName;
                    _oneTime = true;
                    logger.LogInformation("Initiated archive: {Name}", archiveName);
                }
                catch (TokenResponseException e)
                {
                    logger.LogError(e, "InitiateStatusCode: {StatusCode}", e.StatusCode);
                }
                catch (GoogleApiException e)
                {
                    logger.LogError(e, "InitiateStatusCode: {StatusCode}, Archive name: {Name}", e.HttpStatusCode, _name);
                }
            }

            try
            {
                var archiveResult = await importService.CheckJobAsync(_name, stoppingToken);
                logger.LogInformation("Archive name: {Name}, Archive state: {State}", _name, archiveResult.State);
                foreach (var url in archiveResult.Urls)
                {
                    logger.LogInformation("Archive url: {Url}", url);
                }
            }
            catch (GoogleApiException e)
            {
                logger.LogError(e, "CheckStatusCode: {StatusCode}, Archive name: {Name}", e.HttpStatusCode, _name);
            }
            catch (TokenResponseException e)
            {
                logger.LogError(e, "Check StatusCode: {StatusCode}, Archive name: {Name}", e.StatusCode, _name);
            }
        }
    }
}