using System.Security.Claims;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

namespace Pinventory.Identity.Tokens.Grpc.Services;

public class TokenServiceGrpc(TokenService service) : Tokens.TokensBase
{
    private static RpcException NotFound => new(new Status(StatusCode.NotFound, "User or tokens not found"));
    
    public override async Task<TokenResponse> GetAccessToken(UserRequest request, ServerCallContext context)
    {
        var principal = CreatePrincipal(request.UserId);
        var tokens = await service.GetGoogleTokensAsync(principal);

        return tokens is not null
            ? CreateTokenResponse(tokens.AccessToken)
            : throw NotFound;
    }

    public override async Task<TokenResponse> RefreshAccessToken(UserRequest request, ServerCallContext context)
    {
        var principal = CreatePrincipal(request.UserId);
        var token = await service.RefreshGoogleAccessTokenAsync(principal, context.CancellationToken);
        return token is not null
            ? CreateTokenResponse(token)
            : throw NotFound;
    }
    
    private static TokenResponse CreateTokenResponse(GoogleAccessToken token) =>
        new() { AccessToken = token.Token, ExpiresAt = token.ExpiresAt.ToTimestamp() };

    private static ClaimsPrincipal CreatePrincipal(string userId) =>
        new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, userId)]));
}