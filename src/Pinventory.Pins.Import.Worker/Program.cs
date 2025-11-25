using JasperFx.Core;

using Microsoft.EntityFrameworkCore;

using Pinventory.Google;
using Pinventory.Identity.Tokens.Grpc;
using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Import.Worker;
using Pinventory.Pins.Import.Worker.DataPortability;
using Pinventory.Pins.Import.Worker.Handlers;
using Pinventory.Pins.Infrastructure;
using Pinventory.Pins.Infrastructure.Sagas;
using Pinventory.Pins.Infrastructure.Services;
using Pinventory.ServiceDefaults;
using Pinventory.ServiceDefaults.Wolverine;

using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("pinventory-pins-db");
builder.Services.AddDbContextWithWolverineIntegration<PinsDbContext>(options => options.UseNpgsql(connectionString));

builder.UseWolverine(options =>
{
    options.AddDefaultWolverineOptions();

    if (!CodeGeneration.IsGenerating)
    {
        options.PersistMessagesWithPostgresql(connectionString!);
        options.UseRabbitMqUsingNamedConnection("rabbit-mq")
            .EnableWolverineControlQueues()
            .UseConventionalRouting()
            .AutoProvision();
    }

    options.RouteImportProcessingLocally();

    options.Policies.ConventionalLocalRoutingIsAdditive();
    options.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;

    options.Discovery.IncludeType<ImportProcess>();

    options.BatchMessagesOf<ImportPlaceRegistered>(batching =>
    {
        batching.Batcher = new ImportProcessingBatcher();
        batching.BatchSize = ImportProcessingHandler.MaxBatchSize;
        batching.TriggerTime = 1.Seconds();
    }).Sequential();

    options.Services.AddDebugWolverineRouting();
});

builder.Services.AddMemoryCache();

builder.Services.AddGoogleAuthOptions();
builder.Services.AddSingleton<IImportServiceFactory, ImportServiceFactory>();
builder.Services.AddScoped<IImportConcurrencyPolicy, ImportConcurrencyPolicy>();
builder.Services.AddTransient<IArchiveDownloader, ArchiveDownloader>();
builder.Services.AddScoped<IStaredPlaceValidator, StarredPlacesValidator>();

builder.Services.AddGrpcClient<Tokens.TokensClient>(options =>
    options.Address = new Uri("http://pinventory-identity-tokens-grpc")
);

var host = builder.Build();
host.Run();