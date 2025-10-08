using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;

namespace Pinventory.Identity.Tokens;

public sealed class TokenService(UserManager<User> userManager)
{
    private const string TokenType = "token_type";
    private const string ExpiresAt = "expires_at";
    private const string RefreshToken = "refresh_token";
    private const string IdToken = "id_token";
    private const string AccessToken = "access_token";

    public async Task<GoogleTokens?> GetGoogleTokensAsync(ClaimsPrincipal principal)
    {
        User? user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        // pulls from AuthenticationProperties created during external login
        var tokenType = await GetTokenAsync(TokenType);
        var idToken = await GetTokenAsync(IdToken);
        var accessToken = await GetTokenAsync(AccessToken);
        var refreshToken = await GetTokenAsync(RefreshToken);
        var expiresAt = await GetTokenAsync(ExpiresAt);

        if (tokenType is null || idToken is null || accessToken is null || refreshToken is null || expiresAt is null)
        {
            return null;
        }

        return new GoogleTokens(
            new GoogleToken(idToken, tokenType),
            new GoogleAccessToken(accessToken, tokenType, DateTimeOffset.Parse(expiresAt)),
            new GoogleToken(refreshToken, tokenType));

        async Task<string?> GetTokenAsync(string tokenName) =>
            await userManager.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, tokenName);
    }
}