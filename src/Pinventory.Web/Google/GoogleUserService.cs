using Microsoft.AspNetCore.Identity;

using Pinventory.Identity;

namespace Pinventory.Web.Google;

public class GoogleUserService(
    UserManager<User> userManager,
    IHttpContextAccessor httpContextAccessor,
    ILogger<GoogleUserService> logger)
    : IGoogleUserService
{
    public async Task<string?> GetGoogleUserIdAsync()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null || context.User.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("User is not authenticated");
            return null;
        }

        var user = await userManager.GetUserAsync(context.User);

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