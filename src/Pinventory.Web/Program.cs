using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Pinventory.Google;
using Pinventory.Identity;
using Pinventory.Identity.Infrastructure;
using Pinventory.Identity.Tokens;
using Pinventory.ServiceDefaults;
using Pinventory.Web;
using Pinventory.Web.ApiClients;
using Pinventory.Web.Components;
using Pinventory.Web.Google;
using Pinventory.Web.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("pinventory-identity-db")));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<User, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<UserDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.AddGoogleAuthentication();
builder.Services.AddOptions<PinventoryOptions>()
    .BindConfiguration(PinventoryOptions.Section, options => options.ErrorOnUnknownConfiguration = true)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IEmailSender<User>, IdentityNoOpEmailSender>();

builder.Services.AddSingleton<GoogleDataPortabilityClient>();
builder.Services.AddSingleton<IGoogleAuthStateService, GoogleAuthStateService>();

builder.Services.AddTransient<IdTokenHttpMessageHandler>();
builder.Services.AddTransient<TokenService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddPinventoryApiHttpClients();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Incremental Google consent endpoints for Data Portability scope
app.MapGoogleDataPortabilityConsentEndpoints();

await app.RunAsync();