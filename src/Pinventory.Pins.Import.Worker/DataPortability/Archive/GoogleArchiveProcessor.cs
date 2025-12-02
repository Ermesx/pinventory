using System.Text.Json;

using FluentResults;

using Pinventory.Pins.Import.Worker.DataPortability.Archive.Dtos;
using Pinventory.Pins.Infrastructure.Services.Downloading;

namespace Pinventory.Pins.Import.Worker.DataPortability.Archive;

public class GoogleArchiveProcessor(IZipDownloader downloader)
{
    private const string ArchiveBrowserPath = "Portability/archive_browser.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<(ArchiveBrowser Metadata, SavedPlacesCollection Data)>> GetArchiveAsync(Uri archiveBrowserUri,
        Uri dataFilesUri,
        CancellationToken cancellationToken = default)
    {
        var archiveBrowserResult =
            await DownloadAndParseAsync<ArchiveBrowser>(archiveBrowserUri, ArchiveBrowserPath, cancellationToken);
        if (archiveBrowserResult.IsFailed)
        {
            return Result.Fail(archiveBrowserResult.Errors);
        }

        var archiveBrowser = archiveBrowserResult.Value;

        var serviceStatus = archiveBrowser.ServiceStatus.FirstOrDefault();
        if (serviceStatus is null)
        {
            return Result.Fail(Errors.GoogleArchiveProcessor.MissingService());
        }

        var extractedMetadataFile = serviceStatus.ExtractedFile.FirstOrDefault();
        if (extractedMetadataFile is null)
        {
            return Result.Fail(Errors.GoogleArchiveProcessor.MissingExtractedFileMetadata());
        }

        var filePath = $"Portability/{serviceStatus.FolderName}/{extractedMetadataFile.Name}";

        var savedPlacesResult = await DownloadAndParseAsync<SavedPlacesCollection>(dataFilesUri, filePath, cancellationToken);
        return savedPlacesResult.IsSuccess
            ? (archiveBrowser, savedPlacesResult.Value)
            : Result.Fail(savedPlacesResult.Errors);
    }

    private async Task<Result<T>> DownloadAndParseAsync<T>(Uri sourceUri, string filePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await downloader.DownloadAsync(sourceUri, filePath, cancellationToken);
            return await ParseAsync(stream);
        }
        catch (HttpRequestException e)
        {
            return Result.Fail(Errors.GoogleArchiveProcessor.HttpRequestFailed(sourceUri, e.StatusCode).CausedBy(e));
        }
        catch (InvalidDataException e)
        {
            return Result.Fail(Errors.GoogleArchiveProcessor.InvalidContentType(sourceUri).CausedBy(e));
        }
        catch (FileNotFoundException e)
        {
            return Result.Fail(Errors.GoogleArchiveProcessor.FileNotFound(filePath).CausedBy(e));
        }

        async Task<Result<T>> ParseAsync(Stream stream)
        {
            try
            {
                var value = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
                return value is not null
                    ? Result.Ok(value)
                    : Result.Fail(Errors.GoogleArchiveProcessor.FileDeserializationFailed(filePath));
            }
            catch (JsonException e)
            {
                return Result.Fail(Errors.GoogleArchiveProcessor.FileDeserializationFailed(filePath).CausedBy(e));
            }
        }
    }
}