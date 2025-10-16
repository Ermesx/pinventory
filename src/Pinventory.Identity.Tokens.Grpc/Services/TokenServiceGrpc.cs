using System.Security.Claims;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Pinventory.Google.Tokens;

namespace Pinventory.Identity.Tokens.Grpc.Services;

public class TokenServiceGrpc(TokenService service) : Tokens.TokensBase
{
    private static RpcException NotFound => new(new Status(StatusCode.NotFound, "User or tokens not found"));

    public override async Task<TokenResponse> GetAccessToken(UserRequest request, ServerCallContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var principal = CreatePrincipal(request.UserId);
        var tokens = await service.GetGoogleTokensAsync(principal);

        return tokens is not null
            ? CreateTokenResponse(tokens.AccessToken, tokens.DataPortabilityAccessToken)
            : throw NotFound;
    }

    private static TokenResponse CreateTokenResponse(GoogleAccessToken accessToken, GoogleAccessToken? dataPortabilityAccessToken) =>
        new()
        {
            AccessToken =
                new()
                {
                    Token = accessToken.Token,
                    RefreshToken = accessToken.RefreshToken.Token,
                    TokenType = accessToken.TokenType,
                    ExpiresAt = accessToken.ExpiresAt.ToTimestamp()
                },
            DataPortabilityAccessToken = dataPortabilityAccessToken is not null
                ? new()
                {
                    Token = dataPortabilityAccessToken.Token,
                    RefreshToken = dataPortabilityAccessToken.RefreshToken.Token,
                    TokenType = dataPortabilityAccessToken.TokenType,
                    ExpiresAt = dataPortabilityAccessToken.ExpiresAt.ToTimestamp()
                }
                : null
        };

    private static ClaimsPrincipal CreatePrincipal(string userId) =>
        new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, userId)]));
}