using System.Collections.Concurrent;

using FluentResults;

using Microsoft.Extensions.Options;

using Pinventory.Google.Configuration;
using Pinventory.Google.Tokens;
using Pinventory.Identity.Tokens.Grpc;

using TokenResponse = Pinventory.Identity.Tokens.Grpc.TokenResponse;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public sealed class ImportServiceFactory(IOptions<GoogleAuthOptions> options, Tokens.TokensClient client)
    : IImportServiceFactory, IDisposable
{
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
            _ => new ImportService(options, tokens),
            (_, s) =>
            {
                s.Dispose();
                return new ImportService(options, tokens);
            });

        GoogleAccessToken CreateGoogleAccessToken(PairToken token) =>
            GoogleAccessToken.Create(token.Token, token.TokenType, token.RefreshToken, token.ExpiresAt.ToDateTimeOffset());
    }

    public void Dispose()
    {
        foreach (var service in _services.Values)
        {
            service.Dispose();
        }
    }
}