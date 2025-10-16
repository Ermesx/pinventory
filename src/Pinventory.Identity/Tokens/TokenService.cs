using System.Security.Claims;

using FluentResults;

using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;

using Pinventory.Google.Tokens;

namespace Pinventory.Identity.Tokens;

public sealed class TokenService(UserManager<User> userManager)
{
    private const string TokenType = "token_type";
    private const string ExpiresAt = "expires_at";
    private const string RefreshToken = "refresh_token";
    private const string IdToken = "id_token";
    private const string AccessToken = "access_token";

    private const string DataPortabilityProvider = "Google.DataPortability";

    public async Task<GoogleTokens?> GetGoogleTokensAsync(ClaimsPrincipal principal)
    {
        User? user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        // pulls from AuthenticationProperties created during external login
        var tokenType = await GetTokenAsync(TokenType, GoogleDefaults.AuthenticationScheme);
        var idToken = await GetTokenAsync(IdToken, GoogleDefaults.AuthenticationScheme);

        // Google Login AccessToken
        var accessToken = await GetTokenAsync(AccessToken, GoogleDefaults.AuthenticationScheme);
        var refreshToken = await GetTokenAsync(RefreshToken, GoogleDefaults.AuthenticationScheme);
        var expiresAt = await GetTokenAsync(ExpiresAt, GoogleDefaults.AuthenticationScheme);

        if (tokenType is null || idToken is null || accessToken is null || refreshToken is null || expiresAt is null)
        {
            return null;
        }

        // Google Data Portability AccessToken

        var dpAccessToken = await GetTokenAsync(AccessToken, DataPortabilityProvider);
        var dpRefreshToken = await GetTokenAsync(RefreshToken, DataPortabilityProvider);
        var dpTokenType = await GetTokenAsync(TokenType, DataPortabilityProvider);
        var dpExpiresAt = await GetTokenAsync(ExpiresAt, DataPortabilityProvider);

        GoogleAccessToken? dataPortabilityAccessToken =
            dpAccessToken is not null && dpRefreshToken is not null && dpTokenType is not null && dpExpiresAt is not null
                ? GoogleAccessToken.Create(dpAccessToken, dpTokenType, dpRefreshToken, DateTimeOffset.Parse(dpExpiresAt))
                : null;

        return new GoogleTokens(
            new GoogleToken(idToken, tokenType),
            GoogleAccessToken.Create(accessToken, tokenType, refreshToken, DateTimeOffset.Parse(expiresAt)),
            dataPortabilityAccessToken);

        async Task<string?> GetTokenAsync(string tokenName, string provider) =>
            await userManager.GetAuthenticationTokenAsync(user, provider, tokenName);
    }

    public async Task<Result<Success>> SaveGoogleDataPortabilityTokensAsync(ClaimsPrincipal principal, GoogleAccessToken token)
    {
        User? user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Result.Fail(Errors.TokenService.UserNotFound(principal));
        }

        List<IdentityResult> results =
        [
            await SaveTokenAsync(AccessToken, token.Token),
            await SaveTokenAsync(RefreshToken, token.RefreshToken.Token),
            await SaveTokenAsync(TokenType, token.TokenType),
            await SaveTokenAsync(ExpiresAt, token.ExpiresAt.ToString("o"))
        ];

        return results.Any(r => !r.Succeeded)
            ? Result.Fail(Errors.TokenService.SaveFailed(results))
            : Result.Ok();

        async Task<IdentityResult> SaveTokenAsync(string tokenName, string tokenValue) =>
            await userManager.SetAuthenticationTokenAsync(user, DataPortabilityProvider, tokenName, tokenValue);
    }
}