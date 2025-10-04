namespace Pinventory.Web.Tokens;

public record GoogleTokens(GoogleToken IdToken, GoogleAccessToken AccessToken, GoogleToken RefreshToken);