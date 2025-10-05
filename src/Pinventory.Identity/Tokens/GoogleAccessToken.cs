namespace Pinventory.Identity.Tokens;

public record GoogleAccessToken(string Token, string TokenType, DateTimeOffset ExpiresAt) : GoogleToken(Token, TokenType);