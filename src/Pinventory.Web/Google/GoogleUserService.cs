using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

using Pinventory.Identity;

namespace Pinventory.Web.Google;

public class GoogleUserService(
    UserManager<User> userManager,
    IHttpContextAccessor httpContextAccessor,
    ILogger<GoogleUserService> logger,
    IMemoryCache cache)
    : IGoogleUserService
{
    private const int CacheDurationMinutes = 30;

    public async Task<string?> GetGoogleUserIdAsync()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null || context.User.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("User is not authenticated");
            return null;
        }

        var key = $"google-user-id-cache-{context.User.Identity.Name}";
        var principal = context.User;
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(CacheDurationMinutes);
            return await GetGoogleUserIdAsyncInternal(principal);
        });
    }

    private async Task<string?> GetGoogleUserIdAsyncInternal(ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);

        var logins = await userManager.GetLoginsAsync(user!);
        var googleLogin = logins.FirstOrDefault(login => login.LoginProvider == "Google");

        if (googleLogin is null)
        {
            logger.LogError("No Google account linked for user {Email}. Please link your Google account to manage tags", user?.Email);
            return null;
        }

        return googleLogin.ProviderKey;
    }
}