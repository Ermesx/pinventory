using FluentResults;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Pinventory.Google.Configuration;
using Pinventory.Google.Tokens;
using Pinventory.Identity.Tokens.Grpc;
using Pinventory.Pins.Application.Import.Services;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public sealed class ImportServiceFactory(IOptions<GoogleAuthOptions> options, Tokens.TokensClient client, IMemoryCache cache)
    : IImportServiceFactory, IDisposable
{
    private const int MaxAgeMinutes = 10;

    public void Dispose()
    {
        cache.Dispose();
    }

    public async Task<Result<IImportService>> CreateAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(userId, async entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(MaxAgeMinutes));
            entry.RegisterPostEvictionCallback((_, value, _, _) => (value as IDisposable)?.Dispose());

            return await CreateAsyncInternal(userId, cancellationToken);
        }) ?? throw new InvalidOperationException("Failed to create import service");
    }

    private async Task<Result<IImportService>> CreateAsyncInternal(string userId, CancellationToken cancellationToken = default)
    {
        var response = await client.GetAccessTokenAsync(new UserRequest { UserId = userId }, cancellationToken: cancellationToken);
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
    }
}