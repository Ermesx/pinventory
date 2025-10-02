using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.Hosting;

public static class AuthenticationExtensions
{
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
}