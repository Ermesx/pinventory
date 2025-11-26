using System.Net;

using FluentResults;

using Google;
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

public sealed class ImportService(IOptions<GoogleAuthOptions> options, GoogleAccessToken token, ILogger<ImportService> logger)
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
                    Scopes = Scopes,
                }), "user",
            new TokenResponse { AccessToken = token.Token, RefreshToken = token.RefreshToken.Token, ExpiresInSeconds = 3600 }),
        ApplicationName = "Pinventory"
    });

    public void Dispose()
    {
        _service.Dispose();
    }

    public async Task<Result<string>> InitiateAsync(Period? period = null, CancellationToken cancellationToken = default)
    {
        var request = new InitiatePortabilityArchiveRequest
        {
            Resources = Resources, StartTimeDateTimeOffset = period?.Start, EndTimeDateTimeOffset = period?.End,
        };

        try
        {
            var response = await _service.PortabilityArchive.Initiate(request).ExecuteAsync(cancellationToken);
            return response.ArchiveJobId;
        }
        catch (GoogleApiException e) when (e.ToGoogleErrorResponse() is { Error.Status: "ALREADY_EXISTS" } errorResponse)
        {
            logger.LogWarning(e, "Archive job already exists");

            return Result.Fail(Errors.ImportService.ArchiveJobAlreadyExists().CausedBy(e)
                .WithMetadata(Application.Errors.ImportHandler.ArchiveJobExists.ArchiveJobIdMetadataKey,
                    errorResponse.ExtractArchiveJobId()));
        }
        catch (GoogleApiException e)
        {
            logger.LogError(e, "Failed to initiate archive job");
            return Result.Fail(Errors.ImportService.UnexpectedError().CausedBy(e));
        }
    }

    public async Task<Result<(ImportState State, IEnumerable<Uri> Urls)>> CheckJobAsync(string archiveJobId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resource = $"archiveJobs/{archiveJobId}/portabilityArchiveState";
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
        catch (GoogleApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning(e, "Archive job not found: {ArchiveJobId}", archiveJobId);
            return Result.Fail(Errors.ImportService.ArchiveJobNotFound(archiveJobId).CausedBy(e));
        }
    }

    public async Task<Result<Success>> CancelJobAsync(string archiveJobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var resource = $"archiveJobs/{archiveJobId}";
            await _service.ArchiveJobs.Cancel(new CancelPortabilityArchiveRequest(), resource).ExecuteAsync(cancellationToken);
        }
        catch (GoogleApiException e) when (e.HttpStatusCode == HttpStatusCode.BadRequest)
        {
            logger.LogWarning(e, "Cannot cancel job {ArchiveJobId}", archiveJobId);
            return Result.Fail(Errors.ImportService.CannotCancelJob(archiveJobId).CausedBy(e));
        }

        return Result.Ok();
    }

    public async Task DisposeDataArchivesAsync(CancellationToken cancellationToken = default)
    {
        await _service.Authorization.Reset(new ResetAuthorizationRequest()).ExecuteAsync(cancellationToken);
    }
}