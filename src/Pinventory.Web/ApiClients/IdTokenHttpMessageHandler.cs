using System.Net.Http.Headers;

using Pinventory.Identity.Tokens;

namespace Pinventory.Web.ApiClients;

public class IdTokenHttpMessageHandler(TokenService tokenService, IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext!;
        GoogleTokens? tokens = await tokenService.GetGoogleTokensAsync(context.User);
        if (tokens is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.IdToken.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}