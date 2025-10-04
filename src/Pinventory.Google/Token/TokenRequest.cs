using System.Text.Json.Serialization;

namespace Pinventory.Google.Token;

public sealed record TokenRequest(
    [property: JsonPropertyName("client_id")]
    string ClientId,
    [property: JsonPropertyName("client_secret")]
    string ClientSecret,
    [property: JsonPropertyName("refresh_token")]
    string RefreshToken,
    [property: JsonPropertyName("grant_type")]
    string GrantType = "refresh_token"
);