using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Pinventory.Google.Token;
using Pinventory.Identity;
using Pinventory.Identity.Tokens;
using Pinventory.ServiceDefaults;
using Pinventory.Identity.Tokens.Grpc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddTransient<TokenService>();

builder.Services.AddDbContext<UserDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("pinventory-identity-db")));

builder.Services.AddIdentity<User, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<UserDbContext>();

builder.Services.AddHttpClient<IGoogleTokenEndpoint, GoogleTokenEndpoint>()
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(GoogleDefaults.TokenEndpoint));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.MapGrpcService<TokenServiceGrpc>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
