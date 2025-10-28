using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Pinventory.Google.Configuration;

namespace Pinventory.Google;

public static class Extensions
{
    public static OptionsBuilder<GoogleAuthOptions> AddGoogleAuthOptions(this IServiceCollection services) =>
        services.AddOptions<GoogleAuthOptions>()
            .BindConfiguration(GoogleAuthOptions.Section, options => options.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();

    public static GoogleAuthOptions GetGoogleAuthOptions(this IConfigurationManager configuration) =>
        configuration.GetSection(GoogleAuthOptions.Section).Get<GoogleAuthOptions>()!;

    public static OptionsBuilder<GooglePlatformOptions> AddGooglePlatformOptions(this IServiceCollection services) =>
        services.AddOptions<GooglePlatformOptions>()
            .BindConfiguration(GooglePlatformOptions.Section, options => options.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();

    public static GooglePlatformOptions GetGooglePlatformOptions(this IConfigurationManager configuration) =>
        configuration.GetSection(GooglePlatformOptions.Section).Get<GooglePlatformOptions>()!;
}