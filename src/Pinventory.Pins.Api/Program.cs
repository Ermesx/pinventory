using JasperFx;
using JasperFx.CodeGeneration;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using Pinventory.ApiDefaults;
using Pinventory.Pins.Api.Authorization;
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

builder.Services.AddSingleton<IAuthorizationHandler, OwnerMatchesUserAuthorizationHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("OwnerMatchesUser", policy => policy.Requirements.Add(new OwnerMatchesUserRequirement()));

if (!OpenApi.IsGenerating)
{
    var connectionString = builder.Configuration.GetConnectionString("pinventory-pins-db");

    builder.Services.AddDbContextWithWolverineIntegration<PinsDbContext>(options => options.UseNpgsql(connectionString));

    builder.Host.UseWolverine(options =>
    {
        options.Services.AddJasperFx(x =>
        {
            x.Production.ResourceAutoCreate = AutoCreate.None;
            x.Production.GeneratedCodeMode = TypeLoadMode.Static;
            x.Production.AssertAllPreGeneratedTypesExist = true;
        });

        options.UseMemoryPackSerialization();

        options.UseEntityFrameworkCoreTransactions();
        options.PersistMessagesWithPostgresql(connectionString!);

        options.UseRabbitMqUsingNamedConnection("rabbit-mq")
            .EnableWolverineControlQueues()
            .UseConventionalRouting();

        options.Policies.UseDurableOutboxOnAllSendingEndpoints();
        options.Policies.UseDurableInboxOnAllListeners();
        options.Policies.UseDurableLocalQueues();
        options.Policies.ConventionalLocalRoutingIsAdditive();
        options.Policies.AutoApplyTransactions();
    });
}

var app = builder.Build();

app.UseDefaultPipeline();
app.MapApiDefaultEndpoints();

// Configure the HTTP request pipeline
app.MapTagsEndpoints();

app.Run();

public partial class Program
{
}