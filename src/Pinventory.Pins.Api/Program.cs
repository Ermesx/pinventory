using JasperFx.Core;

using Microsoft.EntityFrameworkCore;

using Pinventory.ApiDefaults;
using Pinventory.Pins.Api;
using Pinventory.Pins.Api.Importing;
using Pinventory.Pins.Api.Tags;
using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Infrastructure;
using Pinventory.ServiceDefaults;
using Pinventory.ServiceDefaults.Wolverine;

using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();

// Add services to the container.
builder.Services.AddSignalR();

if (!CodeGeneration.IsGenerating)
{
    var connectionString = builder.Configuration.GetConnectionString("pinventory-pins-db");
    builder.Services.AddDbContextWithWolverineIntegration<PinsDbContext>(options => options.UseNpgsql(connectionString));

    builder.Host.UseWolverine(options =>
    {
        options.AddDefaultWolverineOptions();

        options.PersistMessagesWithPostgresql(connectionString!);
        options.UseRabbitMqUsingNamedConnection("rabbit-mq")
            .EnableWolverineControlQueues()
            .UseConventionalRouting()
            .AutoProvision();

        options.RouteTagCatalogCommandsLocally();

        options.Policies.ConventionalLocalRoutingIsAdditive();

        options.BatchMessagesOf<ImportPlaceProcessed>(batching =>
        {
            batching.BatchSize = ImportProcessingHandler.MaxBatchSize;
            batching.TriggerTime = 1.Seconds();
        }).Sequential();

        options.Services.AddDebugWolverineRouting();
    });
}

var app = builder.Build();

app.UseDefaultPipeline();
app.MapApiDefaultEndpoints();

// Configure the HTTP request pipeline
app.MapTagsEndpoints();
app.MapImportingEndpoints();


app.Run();

namespace Pinventory.Pins.Api
{
    public partial class Program;
}