namespace Pinventory.Web.Google;

/// <summary>
/// Service for retrieving Google user information from ASP.NET Core Identity.
/// </summary>
public interface IGoogleUserService
{
    /// <summary>
    /// Gets the Google user ID (ProviderKey) for the currently authenticated user.
    /// </summary>
    /// <returns>The Google user ID (ProviderKey from AspNetUserLogins).</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when HttpContext is not available or when the user doesn't have a linked Google account.
    /// </exception>
    Task<string> GetGoogleUserIdAsync();
}