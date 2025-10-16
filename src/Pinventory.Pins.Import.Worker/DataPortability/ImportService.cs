using Google.Apis.DataPortability.v1;
using Google.Apis.DataPortability.v1.Data;

using Microsoft.Extensions.Options;

using Pinventory.Google;
using Pinventory.Google.Configuration;
using Pinventory.Google.Tokens;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public sealed class ImportService(IOptions<GoogleAuthOptions> options, GoogleAccessToken token) : IImportService, IDisposable
{
    private static readonly string[] Scopes = [GoogleScopes.DataPortabilityMapsStarredPlaces];
    private static readonly string[] Resources = [GoogleScopes.DataPortabilityResources.MapsStarredPlaces];

    private readonly DataPortabilityService _service = new(new BaseClientService.Initializer
    {
        HttpClientInitializer = new UserCredential(new AuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = options.Value.ClientId,
                        ClientSecret = options.Value.ClientSecret
                    },
                    Scopes = Scopes
                }), "user",
            new TokenResponse
            {
                AccessToken = token.Token,
                RefreshToken = token.RefreshToken.Token,
                ExpiresInSeconds = 3600,
            }),
        ApplicationName = "Pinventory"
    });

    public void Dispose()
    {
        _service.Dispose();
    }

    public async Task<string> InitiateDataArchiveAsync(Period? period = null, CancellationToken cancellationToken = default)
    {
        var initiate = new InitiatePortabilityArchiveRequest
        {
            Resources = Resources,
            StartTimeDateTimeOffset = period?.Start,
            EndTimeDateTimeOffset = period?.End,
        };
        var initResp = await _service.PortabilityArchive.Initiate(initiate).ExecuteAsync(cancellationToken);
        return initResp.ArchiveJobId;
    }

    public async Task<DataArchiveResult> CheckDataArchiveAsync(string archiveJobId, CancellationToken cancellationToken = default)
    {
        var resource = $"archiveJobs/{archiveJobId}/portabilityArchiveState";
        var state = await _service.ArchiveJobs.GetPortabilityArchiveState(resource).ExecuteAsync(cancellationToken);
        return new DataArchiveResult(state.State, state.Urls);
    }
}