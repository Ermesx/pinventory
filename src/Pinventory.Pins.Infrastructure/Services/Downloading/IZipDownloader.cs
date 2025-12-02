namespace Pinventory.Pins.Infrastructure.Services.Downloading;

public interface IZipDownloader
{
    Task<Stream> DownloadAsync(Uri sourceUri, string filePath, CancellationToken cancellationToken = default);
}