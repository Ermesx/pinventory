using Google.Apis.DataPortability.v1;
using Google.Apis.DataPortability.v1.Data;

using Microsoft.Extensions.Options;

using Pinventory.Google;
using Pinventory.Google.Configuration;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public sealed class ImportService(IOptions<GoogleAuthOptions> options, ApiTokens tokens) : IImportService
{
    private static readonly string[] Scopes = [GoogleScopes.DataportabilityMapsStarredPlaces];

    private readonly DataPortabilityService _service = new DataPortabilityService(new BaseClientService.Initializer
    {
        HttpClientInitializer = new UserCredential(new AuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets { ClientId = options.Value.ClientId, ClientSecret = options.Value.ClientSecret },
                    Scopes = Scopes
                }), "user",
            new TokenResponse { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken, ExpiresInSeconds = 3600, }),
        ApplicationName = "DPAPI"
    });
    
    public async Task<string> InitiateDataArchive(Period? period)
    {
        var initiate = new InitiatePortabilityArchiveRequest {
            Resources = Scopes,                       
            StartTimeDateTimeOffset = period?.Start,               
            EndTimeDateTimeOffset = period?.End,                
        };
        var initResp = await _service.PortabilityArchive.Initiate(initiate).ExecuteAsync();
        return initResp.ArchiveJobId;  
    }
    
    public async Task<DataArchiveState> CheckDataArchive(string archiveJobId)
    {
        var state = await _service.ArchiveJobs.GetPortabilityArchiveState(archiveJobId).ExecuteAsync();
        return new DataArchiveState(state.State, state.Urls);
    }
}