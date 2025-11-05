using FluentResults;

using Pinventory.Pins.Application.Importing.Services.Archive;

namespace Pinventory.Pins.Application.Importing.Services;

public interface IArchiveDownloader
{
    Task<Result<(ArchiveBrowser Metadata, SavedPlacesCollection Data)>> DownloadAsync(Uri archiveBrowserUri, Uri dataFilesUri,
        CancellationToken cancellationToken = default);
}