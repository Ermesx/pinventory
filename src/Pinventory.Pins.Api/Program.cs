using JasperFx.Core;

using Microsoft.EntityFrameworkCore;

using Pinventory.ApiDefaults;
using Pinventory.Pins.Api;
using Pinventory.Pins.Api.Importing;
using Pinventory.Pins.Api.Tags;
using Pinventory.Pins.Application;
using Pinventory.Pins.Domain.Abstractions;
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
        options.AddDefaultWolverineOptions("pins_api");

        options.PersistMessagesWithPostgresql(connectionString!);
        options.UseRabbitMqUsingNamedConnection("rabbit-mq")
            .EnableWolverineControlQueues()
            .AutoProvision();

        options.RouteImportCommands();
        options.RouteTagCatalogCommands();

        options.ListenToRabbitQueue("pins.api.events")
            .ConfigureQueue(q => q.BindExchange(PinsMessaging.ExchangeNames.DomainEvents));

        options.Publish(rule =>
        {
            rule.MessagesImplementing<DomainEvent>();
            rule.ToRabbitExchange(PinsMessaging.ExchangeNames.DomainEvents);
        });


        options.BatchMessagesOf<ImportPlaceProcessed>(batching =>
        {
            batching.BatchSize = 25;
            batching.TriggerTime = 1.Seconds();
        }).BufferedInMemory();

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