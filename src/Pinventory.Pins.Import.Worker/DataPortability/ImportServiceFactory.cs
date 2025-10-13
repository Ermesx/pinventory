using System.Collections.Concurrent;

using FluentResults;

using Microsoft.Extensions.Options;

using Pinventory.Google.Configuration;
using Pinventory.Identity.Tokens.Grpc;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public sealed class ImportServiceFactory(IOptions<GoogleAuthOptions> options, Tokens.TokensClient client)
    : IImportServiceFactory, IDisposable
{
    private readonly ConcurrentDictionary<string, ImportService> _services = new();

    public async Task<Result<IImportService>> Create(string userId, CancellationToken cancellationToken = default)
    {
        var response = await client.GetAccessTokenAsync(new UserRequest { UserId = userId }, cancellationToken: cancellationToken);
        if (response is null)
        {
            return Result.Fail(Errors.ImportServiceFactory.NoTokensFoundForUser(userId));
        }

        var tokens = new ApiTokens(response.AccessToken, response.RefreshToken);
        return _services.TryGetValue(userId, out var service)
            ? service
            : _services.AddOrUpdate(userId, _ => new ImportService(options, tokens), (_, s) =>
            {
                s.Dispose();
                return new ImportService(options, tokens);
            });
    }

    public void Dispose()
    {
        foreach (var service in _services.Values)
        {
            service.Dispose();
        }
    }
}