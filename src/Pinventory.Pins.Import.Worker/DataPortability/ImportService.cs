using Google.Apis.DataPortability.v1;
using Google.Apis.DataPortability.v1.Data;

using Microsoft.Extensions.Options;

using Pinventory.Google;
using Pinventory.Google.Configuration;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public sealed class ImportService(IOptions<GoogleAuthOptions> options, ApiTokens tokens) : IImportService, IDisposable
{
    private static readonly string[] Scopes = [GoogleScopes.DataportabilityMapsStarredPlaces];

    private readonly DataPortabilityService _service = new(new BaseClientService.Initializer
    {
        HttpClientInitializer = new UserCredential(new AuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets { ClientId = options.Value.ClientId, ClientSecret = options.Value.ClientSecret },
                    Scopes = Scopes
                }), "user",
            new TokenResponse { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken, ExpiresInSeconds = 3600, }),
        ApplicationName = "Pinventory"
    });

    public void Dispose()
    {
        _service.Dispose();
    }

    public async Task<string> InitiateDataArchive(Period? period, CancellationToken cancellationToken = default)
    {
        var initiate = new InitiatePortabilityArchiveRequest
        {
            Resources = Scopes, StartTimeDateTimeOffset = period?.Start, EndTimeDateTimeOffset = period?.End,
        };
        var initResp = await _service.PortabilityArchive.Initiate(initiate).ExecuteAsync(cancellationToken);
        return initResp.ArchiveJobId;
    }

    public async Task<DataArchiveResult> CheckDataArchive(string archiveJobId, CancellationToken cancellationToken = default)
    {
        var state = await _service.ArchiveJobs.GetPortabilityArchiveState(archiveJobId).ExecuteAsync(cancellationToken);
        return new DataArchiveResult(state.State, state.Urls);
    }
}