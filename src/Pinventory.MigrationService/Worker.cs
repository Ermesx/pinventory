using System.Diagnostics;

using Microsoft.EntityFrameworkCore;

using Pinventory.Identity.Infrastructure;
using Pinventory.Pins.Infrastructure;

namespace Pinventory.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        using var activity = _activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            // TODO: offline database migration for Wolverine https://wolverinefx.net/guide/durability/managing.html#disable-automatic-storage-migration

            using var scope = serviceProvider.CreateScope();
            IEnumerable<DbContext> dbContexts =
            [
                scope.ServiceProvider.GetRequiredService<UserDbContext>(),
                scope.ServiceProvider.GetRequiredService<PinsDbContext>()
            ];

            foreach (var dbContext in dbContexts)
            {
                await RunMigrationAsync(dbContext, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task RunMigrationAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async ct => await dbContext.Database.MigrateAsync(ct), cancellationToken);
    }
}