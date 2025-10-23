using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Pinventory.Pins.Infrastructure;
using Pinventory.Testing.Authorization;
using Pinventory.Testing.Containers;

using TUnit.Core.Interfaces;

using Wolverine;

namespace Pinventory.Pins.Api.IntegrationTests;

public class PinsApiTestApplication : IAsyncInitializer, IAsyncDisposable
{
    private WebApplicationFactory<Program> _factory = null!;
    private IServiceScope _scope = null!;

    [ClassDataSource<PostgresTestContainer>]
    public required PostgresTestContainer Postgres { get; init; }

    [ClassDataSource<RabbitMqTestContainer>]
    public required RabbitMqTestContainer RabbitMq { get; init; }

    public IServiceProvider Services => _factory.Services;
    public HttpClient Client { get; private set; } = null!;
    public PinsDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:pinventory-pins-db", Postgres.ConnectionString);
                builder.UseSetting("ConnectionStrings:rabbit-mq", RabbitMq.ConnectionString);

                builder.ConfigureTestServices(services =>
                {
                    ConfigureAuthentication(services);

                    // Run Wolverine in solo mode for faster test startup
                    services.RunWolverineInSoloMode();

                    // Disable Wolverine persistence to enable db context migration
                    services.DisableAllWolverineMessagePersistence();
                });
            });

        Client = _factory.CreateClient();

        _scope = _factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<PinsDbContext>();

        await InitializeDatabaseAsync(_factory.Services);
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PinsDbContext>();

        // Create the schema first
        await dbContext.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS pins");

        // Then create the tables
        await dbContext.Database.EnsureCreatedAsync();
    }

    private static void ConfigureAuthentication(IServiceCollection services)
    {
        // Replace authentication with test authentication
        services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, _ => { });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PinsDbContext>();

        // Clear all data from tables (if they exist)
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE pins.\"CatalogTags\" CASCADE");
            await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE pins.\"TagCatalogs\" CASCADE");
            await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE pins.\"PinTags\" CASCADE");
            await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE pins.\"Pins\" CASCADE");
        }
        catch
        {
            // Tables don't exist yet, ignore
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await DbContext.DisposeAsync();
        _scope.Dispose();
        await _factory.DisposeAsync();
    }
}