using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Pinventory.Google.Configuration;

namespace Pinventory.Google;

public static class Extensions
{
    public static OptionsBuilder<GoogleAuthOptions> AddGoogleAuthOptions(this IServiceCollection services)
    {
        return services.AddOptions<GoogleAuthOptions>()
            .BindConfiguration(GoogleAuthOptions.Section, options => options.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public static GoogleAuthOptions GetGoogleAuthOptions(this IConfigurationManager configuration)
    {
        return configuration.GetSection(GoogleAuthOptions.Section).Get<GoogleAuthOptions>()!;
    }
}