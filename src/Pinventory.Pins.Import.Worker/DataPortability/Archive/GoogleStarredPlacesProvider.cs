using FluentResults;

using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Import.Worker.DataPortability.Archive.Dtos;

namespace Pinventory.Pins.Import.Worker.DataPortability.Archive;

public class GoogleStarredPlacesProvider(GoogleArchiveProcessor archiveProcessor) : IStarredPlacesProvider
{
    public async Task<Result<IReadOnlyList<StarredPlace>>> ProvideAsync(IReadOnlyList<Uri> urls,
        CancellationToken cancellationToken = default)
    {
        if (urls.Count < 2)
        {
            return Result.Fail(Errors.GoogleStarredPlacesProvider.NotEnoughUrls());
        }

        // Rely on Google behavior that the first URL is the data files and the second is the archive browser
        var dataFilesUri = urls[0];
        var archiveBrowserUri = urls[1];

        var result = await archiveProcessor.GetArchiveAsync(archiveBrowserUri, dataFilesUri, cancellationToken);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        var records = result.Value.Data.Features;
        return records.Select(MapStarredPlace).ToList();
    }

    static StarredPlace MapStarredPlace(Feature place)
    {
        return new StarredPlace(
            place.Properties.Location?.Name,
            place.Properties.GoogleMapsUrl,
            place.Properties.Location?.Address,
            place.Properties.Location?.CountryCode,
            place.Geometry.Coordinates[1],
            place.Geometry.Coordinates[0],
            place.Properties.Date,
            place.Properties.Comment);
    }
}