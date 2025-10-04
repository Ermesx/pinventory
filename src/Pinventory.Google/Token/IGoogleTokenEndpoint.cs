namespace Pinventory.Google.Token;

public interface IGoogleTokenEndpoint
{
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}