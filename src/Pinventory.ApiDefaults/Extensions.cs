using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

using Pinventory.ApiDefaults.Authorization;
using Pinventory.Google;
using Pinventory.ServiceDefaults;

namespace Pinventory.ApiDefaults;

public static class Extensions
{
    public static TBuilder AddApiDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.AddServiceDefaults();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();

        builder.AddJwtBearerGoogleAuthentication();
        builder.AddApiAuthorization();

        builder.Services.AddAuthorization();

        builder.Services.AddHttpContextAccessor();

        return builder;
    }

    public static TBuilder AddJwtBearerGoogleAuthentication<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddGoogleAuthOptions();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                var config = builder.Configuration.GetGoogleAuthOptions();

                o.Authority = "https://accounts.google.com";
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
                    ValidateAudience = true,
                    ValidAudience = config.ClientId,
                    ValidateLifetime = true
                };
                o.IncludeErrorDetails = true;
            });

        return builder;
    }

    public static TBuilder AddApiAuthorization<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddPinventoryOptions();

        builder.Services.AddSingleton<IAuthorizationHandler, OwnerMatchesUserAuthorizationHandler>();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicy.OwnerMatchesUser, policy =>
            {
                var config = builder.Configuration.GetPinventoryOptions();
                policy.AddRequirements(new OwnerMatchesUserRequirement(config.AdminId));
            });

        return builder;
    }

    public static WebApplication UseDefaultPipeline(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        // TODO: Re-enable once https://github.com/dotnet/aspire/issues/10333 - closed in next version
        // app.UseHttpsRedirection();

        return app;
    }

    public static WebApplication MapApiDefaultEndpoints(this WebApplication app)
    {
        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        return app;
    }

    public static string GetIdentifier(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
}