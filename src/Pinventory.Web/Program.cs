using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Pinventory.Web.ApiClients.Notifications.GeneratedCode;
using Pinventory.Web.ApiClients.Pins.GeneratedCode;
using Pinventory.Web.Components;
using Pinventory.Web.Components.Account;
using Pinventory.Web.Model;

using Refit;

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

builder.Services.AddAuthentication(IdentityConstants.ExternalScheme)
    .AddGoogle(options =>
    {
        options.AccessType = "offline";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SaveTokens = true;

        // Extend AuthProperties to store the id_token
        options.Events.OnCreatingTicket = context =>
        {
            const string tokenName = "id_token";
            var idToken = context.TokenResponse.Response!.RootElement.GetString(tokenName);
            context.Properties.Items.Add($".Token.{tokenName}", idToken);

            var tokenNames = context.Properties.Items[".TokenNames"] + ";" + tokenName;
            ;
            context.Properties.Items[".TokenNames"] = tokenNames;

            return Task.CompletedTask;
        };
    });

builder.Services.AddSingleton<IEmailSender<User>, IdentityNoOpEmailSender>();

builder.Services.AddScoped<IdTokenHttpMessageHandler>();

builder.Services.AddRefitClient<IPinsHttpClient>()
    .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://api/pins"))
    .AddHttpMessageHandler<IdTokenHttpMessageHandler>();

builder.Services.AddRefitClient<INotificationsHttpClient>()
    .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://api/notifications"))
    .AddHttpMessageHandler<IdTokenHttpMessageHandler>();

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

app.Run();