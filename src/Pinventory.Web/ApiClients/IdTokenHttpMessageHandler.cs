using System.Net.Http.Headers;

using Pinventory.Web.Components.Account;

public class IdTokenHttpMessageHandler(IdentityUserAccessor userAccessor, IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        GoogleTokens? tokens = await userAccessor.GetGoogleTokensAsync(httpContextAccessor.HttpContext!);
        if (tokens is not null)
        {
            // Add the access token to the outgoing request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.IdToken.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}