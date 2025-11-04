using FluentResults;

using Grpc.Core;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Pinventory.Google.Configuration;
using Pinventory.Google.Tokens;
using Pinventory.Identity.Tokens.Grpc;
using Pinventory.Pins.Application.Importing.Services;

using TokenResponse = Pinventory.Identity.Tokens.Grpc.TokenResponse;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public sealed class ImportServiceFactory(
    IOptions<GoogleAuthOptions> options,
    Tokens.TokensClient client,
    IMemoryCache cache,
    ILogger<ImportServiceFactory> logger)
    : IImportServiceFactory, IDisposable
{
    private const int MaxAgeMinutes = 10;
    private const int MaxAgeSecondsWhenFailed = 10;

    public void Dispose()
    {
        cache.Dispose();
    }

    public async Task<Result<IImportService>> CreateAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(userId, async entry =>
        {
            var key = entry.Key as string;
            entry.RegisterPostEvictionCallback((_, value, _, _) => (value as IDisposable)?.Dispose());

            var result = await CreateAsyncInternal(key!, cancellationToken);

            entry.SetSlidingExpiration(result.IsSuccess
                ? TimeSpan.FromMinutes(MaxAgeMinutes)
                : TimeSpan.FromSeconds(MaxAgeSecondsWhenFailed));

            return result;
        }) ?? throw new InvalidOperationException("Failed to create import service");
    }

    private async ValueTask<Result<IImportService>> CreateAsyncInternal(string userId, CancellationToken cancellationToken = default)
    {
        var response = await GetAccessTokenAsync();
        if (response is null)
        {
            return Result.Fail(Errors.ImportServiceFactory.NoTokensFoundForUser(userId));
        }

        if (response.DataPortabilityAccessToken is null)
        {
            return Result.Fail(Errors.ImportServiceFactory.MissingDataPortabilityToken());
        }

        var tokens = CreateGoogleAccessToken(response.DataPortabilityAccessToken);

        return new ImportService(options, tokens);

        static GoogleAccessToken CreateGoogleAccessToken(PairToken token) =>
            GoogleAccessToken.Create(token.Token, token.TokenType, token.RefreshToken, token.ExpiresAt.ToDateTimeOffset());

        async Task<TokenResponse?> GetAccessTokenAsync()
        {
            try
            {
                return await client.GetAccessTokenAsync(new UserRequest { UserId = userId }, cancellationToken: cancellationToken);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                logger.LogWarning("No tokens found for user {UserId}", userId);
                return null;
            }
        }
    }
}