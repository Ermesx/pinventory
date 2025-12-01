using System.Net.Http.Headers;

using Microsoft.Extensions.Caching.Memory;

using Pinventory.Identity.Tokens;

namespace Pinventory.Web.ApiClients;

public class IdTokenHttpMessageHandler(TokenService tokenService, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
    : DelegatingHandler
{
    private const int MaxAgeMinutes = 10;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext!;
        GoogleTokens? tokens = await cache.GetOrCreateAsync($"google-tokens-cache-{context.User.Identity?.Name}", async entry =>
        {
            var tokens = await tokenService.GetGoogleTokensAsync(context.User);

            entry.SetSlidingExpiration(tokens is not null
                ? TimeSpan.FromMinutes(MaxAgeMinutes)
                : TimeSpan.FromTicks(1));

            return tokens;
        });

        if (tokens is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.IdToken.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}