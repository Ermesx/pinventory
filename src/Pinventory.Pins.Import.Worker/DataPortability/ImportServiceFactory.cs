using System.Collections.Concurrent;

using FluentResults;

using Microsoft.Extensions.Options;

using Pinventory.Google.Configuration;
using Pinventory.Google.Tokens;
using Pinventory.Identity.Tokens.Grpc;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public sealed class ImportServiceFactory(IOptions<GoogleAuthOptions> options, Tokens.TokensClient client, TimeProvider timeProvider)
    : IImportServiceFactory, IDisposable
{
    private const int MaxAgeMinutes = 10;
    private readonly ConcurrentDictionary<string, ImportService> _services = new();

    public async Task<Result<IImportService>> CreateAsync(string userId, CancellationToken cancellationToken = default)
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
        return _services.AddOrUpdate(userId,
            _ => new ImportService(options, tokens, timeProvider),
            (_, s) =>
            {
                s.Dispose();
                return new ImportService(options, tokens, timeProvider);
            });

        static GoogleAccessToken CreateGoogleAccessToken(PairToken token) =>
            GoogleAccessToken.Create(token.Token, token.TokenType, token.RefreshToken, token.ExpiresAt.ToDateTimeOffset());
    }

    public void Destroy(string userId)
    {
        _services.TryGetValue(userId, out var service);
        if (service is not null && service.LastUsed < timeProvider.GetUtcNow().AddMinutes(-MaxAgeMinutes))
        {
            _services.TryRemove(userId, out _);
            service.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var service in _services.Values)
        {
            service.Dispose();
        }
    }
}