using Pinventory.Pins.DataSync.Worker;
using Pinventory.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();
builder.Services.AddGrpcClient<Pinventory.Identity.Tokens.Grpc.Tokens.TokensClient>(options =>
    options.Address = new Uri("http://pinventory-identity-tokens-grpc")
);

var host = builder.Build();
host.Run();