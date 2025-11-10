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

        var tokens = await service.GetGoogleTokensAsync(request.UserId);

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
}