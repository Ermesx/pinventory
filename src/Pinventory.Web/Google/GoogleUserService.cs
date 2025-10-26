using Microsoft.AspNetCore.Identity;

using Pinventory.Identity;
using Pinventory.Web.Identity;

namespace Pinventory.Web.Google;

/// <summary>
/// Service for retrieving Google user information from ASP.NET Core Identity.
/// </summary>
public class GoogleUserService : IGoogleUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IdentityUserAccessor _userAccessor;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GoogleUserService(
        UserManager<User> userManager,
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _userAccessor = userAccessor;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async Task<string> GetGoogleUserIdAsync()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available.");

        var user = await _userAccessor.GetRequiredUserAsync(context);
        var logins = await _userManager.GetLoginsAsync(user);
        var googleLogin = logins.FirstOrDefault(login => login.LoginProvider == "Google");

        if (googleLogin is null)
        {
            throw new InvalidOperationException(
                "No Google account linked. Please link your Google account to manage tags.");
        }

        return googleLogin.ProviderKey;
    }
}