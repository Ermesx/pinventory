using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

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
        builder.Services.AddAuthorization();
        return builder;
    }
    
    public static TBuilder AddJwtBearerGoogleAuthentication<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.Authority = "https://accounts.google.com";
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Authentication:Google:ClientId"],
                    ValidateLifetime = true
                };
                o.MapInboundClaims = false;
                o.IncludeErrorDetails = true;
            });

        return builder;
    }

    public static WebApplication UseDefaultPipeline(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        
        // https://github.com/dotnet/aspire/issues/10333
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
}