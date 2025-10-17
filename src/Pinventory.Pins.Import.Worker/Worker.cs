using Google;

using Pinventory.Pins.Import.Worker.DataPortability;

namespace Pinventory.Pins.Import.Worker;

public class Worker(ILogger<Worker> logger, IImportServiceFactory importServiceFactory) : BackgroundService
{
    private bool _oneTime;
    private string _name = string.Empty;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
            // await TestRun(stoppingToken);
        }
    }
    
    private async Task TestRun(CancellationToken stoppingToken)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            var result = await importServiceFactory.CreateAsync("7d047a61-cae8-45cf-b82e-1d32533bb82d", stoppingToken);
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
                    var archiveName = await importService.InitiateDataArchiveAsync(null, stoppingToken);
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
                var archiveResult = await importService.CheckDataArchiveAsync(_name, stoppingToken);
                logger.LogInformation("Archive name: {Name}, Archive state: {State}", _name, archiveResult.State);
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