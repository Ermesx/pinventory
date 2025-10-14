using Microsoft.EntityFrameworkCore;

using Pinventory.Identity.Infrastructure;
using Pinventory.MigrationService;
using Pinventory.Pins.Infrastructure;
using Pinventory.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("pinventory-identity-db"),
        sqlOptions => sqlOptions.MigrationsAssembly(typeof(Worker).Assembly)));

builder.Services.AddDbContext<PinsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("pinventory-pins-db"),
        sqlOptions => sqlOptions.MigrationsAssembly(typeof(Worker).Assembly)));

var host = builder.Build();
host.Run();