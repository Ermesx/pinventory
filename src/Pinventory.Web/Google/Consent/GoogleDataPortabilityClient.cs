using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;

using Microsoft.Extensions.Options;

using Pinventory.Google;
using Pinventory.Google.Configuration;

namespace Pinventory.Web.Google.Consent;

public sealed class GoogleDataPortabilityClient(IOptions<GoogleAuthOptions> options, ILogger<GoogleDataPortabilityClient> logger) : IDisposable
{
    private readonly GoogleAuthorizationCodeFlow _flow = new(new GoogleAuthorizationCodeFlow.Initializer
    {
        ClientSecrets = new ClientSecrets { ClientId = options.Value.ClientId, ClientSecret = options.Value.ClientSecret },
        Scopes = [GoogleScopes.DataPortabilityMapsStarredPlaces],
        Prompt = "consent",
    });

    public Uri CreateAuthorizationUrlAsync(string redirectUri, string state)
    {
        var request = _flow.CreateAuthorizationCodeRequest(redirectUri);
        request.State = state;

        return request.Build();
    }

    public async Task<TokenResponse> ExchangeCodeForTokenAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        try
        {
            // userId is required by the flow but not used when we don't persist tokens via IDataStore
            const string userId = "dataportability";
            return await _flow.ExchangeCodeForTokenAsync(userId, code, redirectUri, cancellationToken);
        }
        catch (TokenResponseException e)
        {
            logger.LogError(e, "Error exchanging code for token");
            throw;
        }
    }

    public void Dispose()
    {
        _flow.Dispose();
    }
}