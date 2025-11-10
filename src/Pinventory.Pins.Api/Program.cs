using Microsoft.EntityFrameworkCore;

using Pinventory.ApiDefaults;
using Pinventory.Pins.Api.Importing;
using Pinventory.Pins.Api.Tags;
using Pinventory.Pins.Infrastructure;
using Pinventory.ServiceDefaults;

using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();

// Add services to the container.
builder.Services.AddSignalR();

if (!OpenApi.IsGenerating)
{
    var connectionString = builder.Configuration.GetConnectionString("pinventory-pins-db");
    builder.Services.AddDbContextWithWolverineIntegration<PinsDbContext>(options => options.UseNpgsql(connectionString));

    builder.Host.UseWolverine(options =>
    {
        options.PersistMessagesWithPostgresql(connectionString!);

        options.UseRabbitMqUsingNamedConnection("rabbit-mq")
            .EnableWolverineControlQueues()
            .UseConventionalRouting()
            .AutoProvision();

        options.AddDefaultWolverineOptions();
    });
}

var app = builder.Build();

app.UseDefaultPipeline();
app.MapApiDefaultEndpoints();

// Configure the HTTP request pipeline
app.MapTagsEndpoints();
app.MapImportingEndpoints();


app.Run();

public partial class Program;