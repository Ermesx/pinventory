using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Pinventory.Google;

public static class Extensions
{
    public static OptionsBuilder<GoogleAuthOptions> AddGoogleAuthOptions(this IServiceCollection services)
    {
        return services.AddOptions<GoogleAuthOptions>()
            .BindConfiguration(GoogleConfiguration.Section, options => options.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public static GoogleAuthOptions GetGoogleAuthOptions(this IConfigurationManager configuration)
    {
        return configuration.GetSection(GoogleConfiguration.Section).Get<GoogleAuthOptions>()!;
    }

    public static TBuilder AddGoogleAuthentication<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddGoogleAuthOptions();

        builder.Services.AddAuthentication(IdentityConstants.ExternalScheme)
            .AddGoogle(options =>
            {
                var config = builder.Configuration.GetGoogleAuthOptions();

                options.AccessType = "offline";
                options.ClientId = config.ClientId;
                options.ClientSecret = config.ClientSecret;
                options.SaveTokens = true;

                // Extend AuthProperties to store the id_token
                options.Events.OnCreatingTicket = context =>
                {
                    const string tokenName = "id_token";
                    if (context.TokenResponse.Response?.RootElement.TryGetProperty(tokenName, out var tokenElement) == true)
                    {
                        var tokenValue = tokenElement.GetString();

                        if (!string.IsNullOrEmpty(tokenValue))
                        {
                            var tokens = context.Properties.GetTokens().ToList();
                            tokens.Add(new AuthenticationToken { Name = tokenName, Value = tokenValue, });

                            context.Properties.StoreTokens(tokens);
                        }
                    }

                    return Task.CompletedTask;
                };
            });

        return builder;
    }
}