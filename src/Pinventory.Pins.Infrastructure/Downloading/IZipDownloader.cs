namespace Pinventory.Pins.Infrastructure.Downloading;

public interface IZipDownloader
{
    Task<Stream> DownloadAsync(Uri sourceUri, string filePath, CancellationToken cancellationToken = default);
}