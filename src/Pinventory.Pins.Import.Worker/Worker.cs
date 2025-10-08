using Pinventory.Identity.Tokens.Grpc;

namespace Pinventory.Pins.Import.Worker;

public class Worker(ILogger<Worker> logger, Tokens.TokensClient client) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var response = await client.GetAccessTokenAsync(new UserRequest { UserId = "7d047a61-cae8-45cf-b82e-1d32533bb82d" }, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {Time} {AccessToken}", DateTimeOffset.Now, response.AccessToken);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}