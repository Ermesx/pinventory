using System.Text.Json.Serialization;

namespace Pinventory.Google.Token;

public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("expires_in")]
    int ExpiresIn,
    [property: JsonPropertyName("token_type")]
    string TokenType,
    [property: JsonPropertyName("scope")] string? Scope
);