using Google.Apis.DataPortability.v1;
using Google.Apis.DataPortability.v1.Data;

using Microsoft.Extensions.Options;

using Pinventory.Google;
using Pinventory.Google.Configuration;
using Pinventory.Google.Tokens;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public sealed class ImportService(IOptions<GoogleAuthOptions> options, GoogleAccessToken token)
    : IImportService, IDisposable
{
    private static readonly string[] Scopes = [GoogleScopes.DataPortabilityMapsStarredPlaces];
    private static readonly string[] Resources = [GoogleScopes.DataPortabilityResources.MapsStarredPlaces];

    private readonly DataPortabilityService _service = new(new BaseClientService.Initializer
    {
        HttpClientInitializer = new UserCredential(new AuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets { ClientId = options.Value.ClientId, ClientSecret = options.Value.ClientSecret },
                    Scopes = Scopes
                }), "user",
            new TokenResponse { AccessToken = token.Token, RefreshToken = token.RefreshToken.Token, ExpiresInSeconds = 3600 }),
        ApplicationName = "Pinventory"
    });

    public void Dispose()
    {
        _service.Dispose();
    }

    public async Task<string> InitiateAsync(Period? period = null, CancellationToken cancellationToken = default)
    {
        var request = new InitiatePortabilityArchiveRequest
        {
            Resources = Resources, StartTimeDateTimeOffset = period?.Start, EndTimeDateTimeOffset = period?.End,
        };

        var response = await _service.PortabilityArchive.Initiate(request).ExecuteAsync(cancellationToken);
        return response.ArchiveJobId;
    }

    public async Task<(ImportState State, IEnumerable<Uri> Urls)> CheckJobAsync(string archiveJobId,
        CancellationToken cancellationToken = default)
    {
        var resource = CreateArchiveResource(archiveJobId);
        var response = await _service.ArchiveJobs.GetPortabilityArchiveState(resource).ExecuteAsync(cancellationToken);

        var state = response.State switch
        {
            "IN_PROGRESS" => ImportState.InProgress,
            "COMPLETE" => ImportState.Complete,
            "FAILED" => ImportState.Failed,
            "CANCELLED" => ImportState.Cancelled,
            _ => ImportState.Unspecified
        };

        if (state != ImportState.Complete)
        {
            return (state, []);
        }

        var urls = response.Urls.Select(x => new Uri(x)).ToList();
        return (state, urls);
    }

    public async Task CancelJobAsync(string archiveJobId, CancellationToken cancellationToken = default)
    {
        var resource = CreateArchiveResource(archiveJobId);
        await _service.ArchiveJobs.Cancel(new CancelPortabilityArchiveRequest(), resource).ExecuteAsync(cancellationToken);
    }

    public async Task DisposeDataArchivesAsync(CancellationToken cancellationToken = default)
    {
        await _service.Authorization.Reset(new ResetAuthorizationRequest()).ExecuteAsync(cancellationToken);
    }

    private static string CreateArchiveResource(string archiveJobId) => $"archiveJobs/{archiveJobId}/portabilityArchiveState";
}