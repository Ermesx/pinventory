using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Import.Messages;
using Pinventory.Pins.Domain.Import;
using Pinventory.Pins.Infrastructure;

using Wolverine;

namespace Pinventory.Pins.Application.Import;

public class PinsImport : Saga
{
    public string? Id { get; set; }
    public ImportJobState State { get; set; } = ImportJobState.Unspecified;


    public async Task<IEnumerable<ProcessPinMessage>> DownloadArchiveAsync(
        DownloadArchiveMessage download,
        PinsDbContext dbContext,
        ILogger<PinsImport> logger)
    {
        logger.LogInformation("Downloading archive {Url}", download.Url);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(download.Url);

        var starredPlaces = response.Content.ReadFromJsonAsAsyncEnumerable<StarredPlace>(); // What is the structure of the archive?
        await foreach (StarredPlace? place in starredPlaces)
        {
            // var pin = new Pin(null, new GooglePlaceId(place.PlaceId), new Address(place.Address), new Location(place.Lat, place.Lng));
            // dbContext.Pins.Add(pin);
        }

        await dbContext.SaveChangesAsync();

        return [];
    }
}