using FluentResults;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace Pinventory.Web.Google;

public sealed class GoogleAuthStateService(IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor)
    : IGoogleAuthStateService
{
    private const string CookieName = "gdp_state";
    private const string ProtectorPurpose = "Google.Dataportability.Consent";
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(5);
    private readonly PropertiesDataFormat _protector = new(dataProtectionProvider.CreateProtector(ProtectorPurpose));

    public string CreateAndStoreState(AuthenticationProperties properties, TimeSpan? lifetime = null)
    {
        var state = _protector.Protect(properties);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.Add(lifetime ?? DefaultLifetime)
        };

        var context = httpContextAccessor.HttpContext!;
        context.Response.Cookies.Append(CookieName, state, cookieOptions);
        return state;
    }

    public Result<AuthenticationProperties> ValidateAndClearState(string stateFromQuery)
    {
        // Retrieve cookie
        var context = httpContextAccessor.HttpContext!;
        if (!context.Request.Cookies.TryGetValue(CookieName, out var state))
        {
            // Ensure cookie is cleared even if missing or invalid to avoid reuse attempts
            ClearStateCookie();
            return Result.Fail(Errors.GoogleAuthState.StateMissing());
        }

        // Compare
        if (!string.Equals(state, stateFromQuery, StringComparison.Ordinal))
        {
            ClearStateCookie();
            return Result.Fail(Errors.GoogleAuthState.StateMismatch());
        }

        // Unprotect
        AuthenticationProperties? properties;
        try
        {
            properties = _protector.Unprotect(state);
        }
        catch
        {
            ClearStateCookie();
            return Result.Fail(Errors.GoogleAuthState.StateInvalid());
        }

        if (properties is null)
        {
            ClearStateCookie();
            return Result.Fail(Errors.GoogleAuthState.PropertiesMissing());
        }

        // Success - clear cookie after use
        ClearStateCookie();
        return Result.Ok(properties);
    }

    public void ClearStateCookie()
    {
        var deleteOptions = new CookieOptions { SameSite = SameSiteMode.None, Secure = true };
        var context = httpContextAccessor.HttpContext!;
        context.Response.Cookies.Delete(CookieName, deleteOptions);
    }
}