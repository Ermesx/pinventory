using Microsoft.EntityFrameworkCore;

using Pinventory.Google;
using Pinventory.Identity.Tokens.Grpc;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Import.Worker.DataPortability;
using Pinventory.Pins.Infrastructure;
using Pinventory.Pins.Infrastructure.Services;
using Pinventory.ServiceDefaults;

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
    options.PersistMessagesWithPostgresql(connectionString!);

    options.UseRabbitMqUsingNamedConnection("rabbit-mq")
        .EnableWolverineControlQueues()
        .UseConventionalRouting();

    options.AddDefaultWolverineOptions();
});

builder.Services.AddMemoryCache();

builder.Services.AddGoogleAuthOptions();
builder.Services.AddSingleton<IImportServiceFactory, ImportServiceFactory>();
builder.Services.AddScoped<IImportConcurrencyPolicy, ImportConcurrencyPolicy>();
builder.Services.AddTransient<IArchiveDownloader, ArchiveDownloader>();

builder.Services.AddGrpcClient<Tokens.TokensClient>(options =>
    options.Address = new Uri("http://pinventory-identity-tokens-grpc")
);

var host = builder.Build();
host.Run();