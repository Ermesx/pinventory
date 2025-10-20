using JasperFx.Resources;

using Microsoft.EntityFrameworkCore;

using Pinventory.ApiDefaults;
using Pinventory.Pins.Api.Tags;
using Pinventory.Pins.Infrastructure;

using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.MemoryPack;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();

// Add services to the container.
if (!OpenApi.IsGenerating)
{
    var connectionString = builder.Configuration.GetConnectionString("pinventory-pins-db");

    builder.Services.AddDbContext<PinsDbContext>(options =>
        options.UseNpgsql(connectionString), optionsLifetime: ServiceLifetime.Singleton);

    builder.Host.UseWolverine(options =>
    {
        options.Services.AddResourceSetupOnStartup();

        options.UseMemoryPackSerialization();

        options.PersistMessagesWithPostgresql(connectionString!);
        options.UseEntityFrameworkCoreTransactions();

        var rabbitConnectionString = builder.Configuration.GetConnectionString("rabbit-mq");
        if (!string.IsNullOrWhiteSpace(rabbitConnectionString))
        {
            options.UseRabbitMqUsingNamedConnection("rabbit-mq")
                .EnableWolverineControlQueues()
                .AutoProvision()
                .UseConventionalRouting();

            options.Policies.UseDurableOutboxOnAllSendingEndpoints();
            options.Policies.UseDurableInboxOnAllListeners();
        }

        options.Policies.ConventionalLocalRoutingIsAdditive();
        options.Policies.AutoApplyTransactions();
        options.Policies.UseDurableLocalQueues();
    });
}

var app = builder.Build();

app.UseDefaultPipeline();
app.MapApiDefaultEndpoints();

// Configure the HTTP request pipeline
app.MapTagsEndpoints();

await app.RunAsync();