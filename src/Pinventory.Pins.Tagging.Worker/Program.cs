using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Infrastructure;
using Pinventory.Pins.Infrastructure.Services;
using Pinventory.ServiceDefaults;
using Pinventory.Tagging.Worker;

using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<Worker>();

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("pinventory-pins-db");
builder.Services.AddDbContextWithWolverineIntegration<PinsDbContext>(options => options.UseNpgsql(connectionString));

builder.UseWolverine(options =>
{
    options.PersistMessagesWithPostgresql(connectionString!);

    options.UseRabbitMqUsingNamedConnection("rabbit-mq")
        .EnableWolverineControlQueues()
        .UseConventionalRouting();

    options.AddDefaultWolverineOptions();
});

builder.Services.AddScoped<ITagVerifier, TagVerifier>();

var host = builder.Build();
host.Run();