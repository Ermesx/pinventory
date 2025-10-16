using Pinventory.Google.Tokens;

namespace Pinventory.Identity.Tokens;

public record GoogleTokens(GoogleToken IdToken, GoogleAccessToken AccessToken, GoogleAccessToken? DataPortabilityAccessToken);