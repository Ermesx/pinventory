using Microsoft.EntityFrameworkCore;

using Pinventory.Identity;
using Pinventory.MigrationService;
using Pinventory.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("pinventory-identity-db"),
        sqlOptions => sqlOptions.MigrationsAssembly(typeof(Worker).Assembly)));

var host = builder.Build();
host.Run();