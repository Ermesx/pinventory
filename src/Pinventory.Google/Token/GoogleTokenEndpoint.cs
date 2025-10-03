using System.Net.Http.Json;

using Microsoft.Extensions.Options;

namespace Pinventory.Google.Token;

public class GoogleTokenEndpoint(HttpClient httpClient, IOptions<GoogleAuthOptions> googleOptions) : IGoogleTokenEndpoint
{
    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        TokenRequest request = new(
            googleOptions.Value.ClientId,
            googleOptions.Value.ClientSecret,
            refreshToken);

        var response = await httpClient.PostAsJsonAsync("/token", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken) ?? null;
    }
}