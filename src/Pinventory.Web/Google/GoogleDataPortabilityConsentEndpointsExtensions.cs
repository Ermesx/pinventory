using Google.Apis.Auth.OAuth2.Responses;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

using Pinventory.Google.Tokens;
using Pinventory.Identity.Tokens;

namespace Pinventory.Web.Google;

public static class GoogleDataPortabilityConsentEndpointsRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapGoogleDataPortabilityConsentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/Google").RequireAuthorization();

        group.MapPost("/PerformGoogleDataPortabilityConsent", (
            HttpContext context,
            [FromServices] IGoogleAuthStateService stateService,
            [FromServices] GoogleDataPortabilityClient client,
            [FromForm] string returnUrl) =>
        {
            var properties = new AuthenticationProperties { RedirectUri = returnUrl };
            var state = stateService.CreateAndStoreState(properties);
            
            var redirectUri = CreateRedirectUri(context);
            var authUrl = client.CreateAuthorizationUrlAsync(redirectUri, state);
            
            return Results.Redirect(authUrl.ToString());
        });

        // Handle Google redirect back and persist updated tokens (manual flow)
        group.MapGet("/ConsentCallback", async (
            HttpContext context,
            [FromServices] IGoogleAuthStateService stateService,
            [FromServices] GoogleDataPortabilityClient client,
            [FromServices] TokenService tokenService) =>
        {
            var state = context.Request.Query["state"].ToString();
            var stateValidation = stateService.ValidateAndClearState(state);
            if (stateValidation.IsFailed)
            {
                var target = AppendQuery("/Error", "message", stateValidation.Errors.First().Message);
                return Results.Redirect(target);
            }

            var returnUrl = stateValidation.Value.RedirectUri;
            if (returnUrl is null)
            {
                var target = AppendQuery("/Error", "message", stateValidation.Errors.First().Message);
                return Results.Redirect(target);
            }

            var error = context.Request.Query["error"].ToString();
            if (!string.IsNullOrEmpty(error))
            {
                var target = AppendQuery(returnUrl, "consentDenied", "1");
                return Results.Redirect(target);
            }

            var code = context.Request.Query["code"].ToString();

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                var target = AppendQuery(returnUrl, "consentError", "invalid_response");
                return Results.Redirect(target);
            }
            
            TokenResponse tokenResponse;
            try
            {
                var redirectUri = CreateRedirectUri(context);
                tokenResponse = await client.ExchangeCodeForTokenAsync(code, redirectUri);
            }
            catch (TokenResponseException e)
            {
                var target = AppendQuery(returnUrl, "consentError", "token_exchange_failed");
                return Results.Redirect(target);
            }

            var token = CreateToken(tokenResponse);
            var saveResult = await tokenService.SaveGoogleDataPortabilityTokensAsync(context.User, token);
            if (saveResult.IsFailed)
            {
                var target = AppendQuery(returnUrl, "consentError", "save_failed");
                return Results.Redirect(target);
            }

            return Results.Redirect(returnUrl);
        });

        return group;
    }
    
    private static string CreateRedirectUri(HttpContext context) =>
        UriHelper.BuildAbsolute(
            context.Request.Scheme,
            context.Request.Host,
            context.Request.PathBase,
            "/Google/ConsentCallback");

    private static GoogleAccessToken CreateToken(TokenResponse tokenResponse) =>
        new(tokenResponse.AccessToken, tokenResponse.TokenType,
            new GoogleToken(tokenResponse.RefreshToken, GoogleAccessToken.RefreshTokenName),
            DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 0));

    private static string AppendQuery(string url, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(url)) url = "/";
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }
}