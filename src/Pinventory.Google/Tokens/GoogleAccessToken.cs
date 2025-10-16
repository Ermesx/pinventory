
namespace Pinventory.Google.Tokens;

public record GoogleAccessToken(string Token, string TokenType, GoogleToken RefreshToken, DateTimeOffset ExpiresAt)
    : GoogleToken(Token, TokenType)
{
    public const string RefreshTokenName = "refresh_token";

    public static GoogleAccessToken Create(string token, string tokenType, string refreshToken, DateTimeOffset expiresAt) =>
        new(token, tokenType, new GoogleToken(refreshToken, tokenType), expiresAt);
}