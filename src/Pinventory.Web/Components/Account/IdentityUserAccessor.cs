using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;

using Pinventory.Web.Model;

namespace Pinventory.Web.Components.Account;

record GoogleToken(string Token, string TokenType);

record GoogleAccessToken(string Token, string TokenType, DateTime ExpiresAt) : GoogleToken(Token, TokenType);

record GoogleTokens(GoogleToken IdToken, GoogleAccessToken AccessToken, GoogleToken RefreshToken);

internal sealed class IdentityUserAccessor(UserManager<User> userManager, IdentityRedirectManager redirectManager)
{
    public async Task<User> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser",
                $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }

    public async Task<GoogleTokens?> GetGoogleTokensAsync(HttpContext context)
    {
        User? user = await userManager.GetUserAsync(context.User);
        if (user is null)
        {
            return null;
        }

        // pulls from AuthenticationProperties created during external login
        var tokenType = await GetTokenAsync("token_type");
        var idToken = await GetTokenAsync("id_token") ?? "null-token";
        var accessToken = await GetTokenAsync("access_token");
        var refreshToken = await GetTokenAsync("refresh_token");
        var expiresAt = await GetTokenAsync("expires_at");

        if (tokenType is null || idToken is null || accessToken is null || refreshToken is null || expiresAt is null)
        {
            return null;
        }

        return new GoogleTokens(
            new GoogleToken(idToken, tokenType),
            new GoogleAccessToken(accessToken, tokenType, DateTime.Parse(expiresAt)),
            new GoogleToken(refreshToken, tokenType));

        async Task<string?> GetTokenAsync(string tokenName) =>
            await userManager.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, tokenName);
    }
}