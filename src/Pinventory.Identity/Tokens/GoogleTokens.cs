namespace Pinventory.Identity.Tokens;

public record GoogleTokens(GoogleToken IdToken, GoogleAccessToken AccessToken, GoogleToken RefreshToken);