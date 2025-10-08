using Pinventory.Google;
using Pinventory.Pins.Import.Worker;
using Pinventory.Pins.Import.Worker.DataPortability;
using Pinventory.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddGoogleAuthOptions();
builder.Services.AddSingleton<IImportServiceFactory, ImportServiceFactory>();

builder.Services.AddHostedService<Worker>();
builder.Services.AddGrpcClient<Pinventory.Identity.Tokens.Grpc.Tokens.TokensClient>(options =>
    options.Address = new Uri("http://pinventory-identity-tokens-grpc")
);

var host = builder.Build();
host.Run();