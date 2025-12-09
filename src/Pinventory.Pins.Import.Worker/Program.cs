using JasperFx.Core;

using Microsoft.EntityFrameworkCore;

using Pinventory.Google;
using Pinventory.Identity.Tokens.Grpc;
using Pinventory.Pins.Application;
using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain.Abstractions;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Import.Worker;
using Pinventory.Pins.Import.Worker.DataPortability;
using Pinventory.Pins.Import.Worker.DataPortability.Archive;
using Pinventory.Pins.Import.Worker.Handlers;
using Pinventory.Pins.Infrastructure;
using Pinventory.Pins.Infrastructure.Downloading;
using Pinventory.Pins.Infrastructure.Importing.Messages;
using Pinventory.Pins.Infrastructure.Importing.Middlewares;
using Pinventory.Pins.Infrastructure.Importing.Sagas;
using Pinventory.Pins.Infrastructure.Importing.Services;
using Pinventory.Pins.Infrastructure.Messages;
using Pinventory.Pins.Infrastructure.Middlewares;
using Pinventory.ServiceDefaults;
using Pinventory.ServiceDefaults.Wolverine;

using Wolverine;
using Wolverine.Configuration;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
if (!CodeGeneration.IsGenerating)
{
    var connectionString = builder.Configuration.GetConnectionString("pinventory-pins-db");
    builder.Services.AddDbContextWithWolverineIntegration<PinsDbContext>(options => options.UseNpgsql(connectionString));

    builder.UseWolverine(options =>
    {
        options.AddDefaultWolverineOptions("pins_import_worker");

        options.PersistMessagesWithPostgresql(connectionString!);
        options.UseRabbitMqUsingNamedConnection("rabbit-mq")
            .EnableWolverineControlQueues()
            .AutoProvision();

        options.RouteImportProcessing();

        options.ListenToRabbitQueue("pins.import.worker.events")
            .ConfigureQueue(q => q.BindExchange(PinsMessaging.ExchangeNames.DomainEvents))
            .PartitionProcessingByGroupId(PartitionSlots.Five);

        options.Publish(rule =>
        {
            rule.MessagesImplementing<DomainEvent>();
            rule.ToRabbitExchange(PinsMessaging.ExchangeNames.DomainEvents);
        });

        options.MessagePartitioning.UseInferredMessageGrouping();

        options.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;

        options.Discovery.IncludeType<ImportProcess>();

        options.BatchMessagesOf<ImportPlaceRegistered>(batching =>
        {
            batching.Batcher = new ImportProcessingBatcher();
            batching.BatchSize = ImportProcessingHandler.MaxBatchSize;
            batching.TriggerTime = 1.Seconds();
        }).UseDurableInbox();

        options.Policies.AddMiddleware<DomainEventsPublisherMiddleware>();
        options.Policies.ForMessagesOfType<IUserMessage>().AddMiddleware<CurrentImportLoaderMiddleware>();

        options.Services.AddDebugWolverineRouting();
    });
}

builder.Services.AddMemoryCache();

builder.Services.AddGoogleAuthOptions();
builder.Services.AddSingleton<IImportServiceFactory, ImportServiceFactory>();
builder.Services.AddScoped<IImportConcurrencyPolicy, ImportConcurrencyPolicy>();
builder.Services.AddScoped<IStarredPlaceValidator, StarredPlacesValidator>();

builder.Services.AddTransient<IZipDownloader, HttpZipDownloader>();
builder.Services.AddTransient<GoogleArchiveProcessor>();
builder.Services.AddTransient<IStarredPlacesProvider, GoogleStarredPlacesProvider>();

builder.Services.AddGrpcClient<Tokens.TokensClient>(options =>
    options.Address = new Uri("http://pinventory-identity-tokens-grpc")
);

var host = builder.Build();
host.Run();