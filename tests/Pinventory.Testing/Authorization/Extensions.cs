using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Pinventory.Testing.Authorization;

public static class Extensions
{
    public static AuthenticationBuilder AddTestAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<CurrentUserIdProvider>();

        services.AddSingleton<AuthenticationSchemeOptions>();
        return services.AddAuthentication(AuthenticationTestHandler.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, AuthenticationTestHandler>(AuthenticationTestHandler.AuthenticationScheme,
                _ => { });
    }
}