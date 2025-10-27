using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Pinventory.Pins.Infrastructure;
using Pinventory.Testing.Abstractions;
using Pinventory.Testing.Authorization;
using Pinventory.Testing.Containers;

using TUnit.Core.Interfaces;

using Wolverine;

namespace Pinventory.Pins.Api.IntegrationTests;

public class PinsApiTestApplication : ApplicationWithDatabase<PinsDbContext>, IAsyncInitializer, IAsyncDisposable
{
    private WebApplicationFactory<Program> _factory = null!;
    private IServiceScope _scope = null!;

    [ClassDataSource<PostgresTestContainer>]
    public required PostgresTestContainer Postgres { get; init; }

    [ClassDataSource<RabbitMqTestContainer>]
    public required RabbitMqTestContainer RabbitMq { get; init; }

    public IServiceProvider Services => _factory.Services;
    public HttpClient Client { get; private set; } = null!;

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await DbContext.DisposeAsync();
        _scope.Dispose();
        await _factory.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:pinventory-pins-db", Postgres.ConnectionString);
                builder.UseSetting("ConnectionStrings:rabbit-mq", RabbitMq.ConnectionString);

                builder.ConfigureTestServices(services =>
                {
                    services.AddTestAuthentication();

                    // Run Wolverine in solo mode for faster test startup
                    services.RunWolverineInSoloMode();

                    // Disable Wolverine persistence to enable db context migration
                    services.DisableAllWolverineMessagePersistence();
                    services.DisableAllExternalWolverineTransports();
                });
            });

        Client = _factory.CreateClient();

        _scope = _factory.Services.CreateScope();
        var dbContext = _scope.ServiceProvider.GetRequiredService<PinsDbContext>();

        await InitializeDatabaseAsync(dbContext);
    }
}