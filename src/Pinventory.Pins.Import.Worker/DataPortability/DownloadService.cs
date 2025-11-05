using System.IO.Compression;
using System.Text.Json;

using FluentResults;

using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Application.Importing.Services.Archive;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public class ArchiveDownloader(IHttpClientFactory httpClientFactory) : IArchiveDownloader
{
    private const string ArchiveBrowserPath = "Portability/archive_browser.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<(ArchiveBrowser Metadata, SavedPlacesCollection Data)>> DownloadAsync(Uri archiveBrowserUri, Uri dataFilesUri,
        CancellationToken cancellationToken = default)
    {
        var archiveBrowserResult =
            await DownloadAndParseEntryAsync<ArchiveBrowser>(archiveBrowserUri, ArchiveBrowserPath, cancellationToken);
        if (archiveBrowserResult.IsFailed)
        {
            return Result.Fail(archiveBrowserResult.Errors);
        }

        var archiveBrowser = archiveBrowserResult.Value;

        var serviceStatus = archiveBrowser.ServiceStatus.FirstOrDefault();
        if (serviceStatus is null)
        {
            return Result.Fail(Errors.ArchiveDownload.MissingService());
        }

        var extractedMetadataFile = serviceStatus.ExtractedFile.FirstOrDefault();
        if (extractedMetadataFile is null)
        {
            return Result.Fail(Errors.ArchiveDownload.MissingExtractedFileMetadata());
        }

        var filePath = $"Portability/{serviceStatus.FolderName}/{extractedMetadataFile.Name}";

        var savedPlacesResult = await DownloadAndParseEntryAsync<SavedPlacesCollection>(dataFilesUri, filePath, cancellationToken);
        return savedPlacesResult.IsSuccess
            ? (archiveBrowser, savedPlacesResult.Value)
            : Result.Fail(savedPlacesResult.Errors);
    }

    private async Task<Result<T>> DownloadAndParseEntryAsync<T>(Uri sourceUri, string filePath, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(sourceUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Result.Fail(Errors.ArchiveDownload.HttpRequestFailed(response));
        }

        await using var zipStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.GetEntry(filePath);
        if (entry is null)
        {
            return Result.Fail(Errors.ArchiveDownload.FileNotFound(filePath));
        }

        await using var entryStream = entry.Open();
        try
        {
            var value = await JsonSerializer.DeserializeAsync<T>(entryStream, JsonOptions, cancellationToken);
            return value is not null
                ? Result.Ok(value)
                : Result.Fail(Errors.ArchiveDownload.FileDeserializationFailed(filePath));
        }
        catch (JsonException)
        {
            return Result.Fail(Errors.ArchiveDownload.FileDeserializationFailed(filePath));
        }
    }
}