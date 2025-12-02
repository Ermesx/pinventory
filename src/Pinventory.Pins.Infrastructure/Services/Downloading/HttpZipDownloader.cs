using System.IO.Compression;

namespace Pinventory.Pins.Infrastructure.Services.Downloading;

public class HttpZipDownloader(IHttpClientFactory httpClientFactory) : IZipDownloader
{
    public async Task<Stream> DownloadAsync(Uri sourceUri, string filePath, CancellationToken cancellationToken = default)
    {
        var (stream, contentType) = await DownloadAsync(sourceUri, cancellationToken);
        if (!contentType.StartsWith("application/zip"))
        {
            throw new InvalidDataException($"Invalid content type {contentType} for {sourceUri}");
        }

        await using (stream)
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var entry = archive.GetEntry(filePath) ?? throw new FileNotFoundException($"Could not find '{filePath}' in the ZIP file");
            await using var entryStream = entry.Open();

            // Copy entry stream to memory stream
            var memoryStream = new MemoryStream();
            await entryStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }

    private async Task<(Stream file, string contentType)> DownloadAsync(Uri uri, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(uri, cancellationToken);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadAsStreamAsync(cancellationToken), response.Content.Headers.ContentType?.ToString() ??
                                                                             "application/octet-stream");
    }
}