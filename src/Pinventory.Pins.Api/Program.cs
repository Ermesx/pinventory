using Pinventory.ApiDefaults;
using Pinventory.Pins.Api.TagsCatalog;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();

// Add services to the container.

var app = builder.Build();

app.UseDefaultPipeline();
app.MapApiDefaultEndpoints();

// Configure the HTTP request pipeline
app.MapTagsCatalogEndpoints();

app.Run();