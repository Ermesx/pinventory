using System.Text;
using System.Text.Json;

using Moq;

using Nager.Country;

using Pinventory.Pins.Import.Worker.DataPortability.Archive;
using Pinventory.Pins.Import.Worker.DataPortability.Archive.Dtos;
using Pinventory.Pins.Infrastructure.Services.Downloading;

using Shouldly;

namespace Pinventory.Pins.Import.Worker.UnitTests.DataPortability.Archive;

public class GoogleStarredPlacesProviderTests
{
    [Test]
    public async Task ProvideAsync_returns_failure_when_not_enough_urls()
    {
        // Arrange
        var downloaderMock = new Mock<IZipDownloader>();
        var archiveProcessor = new GoogleArchiveProcessor(downloaderMock.Object);
        var sut = new GoogleStarredPlacesProvider(archiveProcessor);

        var urls = Array.Empty<Uri>();

        // Act
        var result = await sut.ProvideAsync(urls, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Not enough URLs to download archive"));
    }

    [Test]
    public async Task ProvideAsync_returns_failure_when_archive_processor_fails()
    {
        // Arrange
        var downloaderMock = new Mock<IZipDownloader>();
        var archiveProcessor = new GoogleArchiveProcessor(downloaderMock.Object);
        var sut = new GoogleStarredPlacesProvider(archiveProcessor);

        var urls = new[] { new Uri("https://data"), new Uri("https://browser") };

        downloaderMock
            .Setup(d => d.DownloadAsync(urls[1], "Portability/archive_browser.json", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("error"));

        // Act
        var result = await sut.ProvideAsync(urls, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task ProvideAsync_maps_places_when_successful()
    {
        // Arrange
        var downloaderMock = new Mock<IZipDownloader>();
        var archiveProcessor = new GoogleArchiveProcessor(downloaderMock.Object);
        var sut = new GoogleStarredPlacesProvider(archiveProcessor);

        var urls = new[] { new Uri("https://data"), new Uri("https://browser") };

        var feature = new Feature(
            new Geometry([21.0, 52.0], "Point"),
            new Properties(
                DateTimeOffset.UtcNow,
                "https://maps.google.com/1",
                new LocationAndName("Address", Alpha2Code.PL, "Place name"),
                "Nice place"),
            "Feature");

        var collection = new SavedPlacesCollection("FeatureCollection", [feature]);

        var archiveBrowser = new ArchiveBrowser(
            "2024-01-01T00:00:00Z",
            "100MB",
            [
                new ServiceStatus(
                    new Service("Maps"),
                    "Google Maps",
                    false,
                    [new ExtractedFile("saved_places.json", ".json", "COMPLETE", "saved_places")],
                    "folder",
                    new ServiceInformation(
                        "Maps",
                        new BriefDescription("desc"),
                        new PromoText("promo"),
                        new FolderStructure("structure"),
                        [],
                        []),
                    "0",
                    "1",
                    "1KB")
            ]);

        using var archiveBrowserStream = CreateJsonStream(archiveBrowser);
        using var savedPlacesStream = CreateJsonStream(collection);

        downloaderMock
            .Setup(d => d.DownloadAsync(urls[1], "Portability/archive_browser.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(archiveBrowserStream);
        downloaderMock
            .Setup(d => d.DownloadAsync(urls[0], "Portability/folder/saved_places.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedPlacesStream);

        // Act
        var result = await sut.ProvideAsync(urls, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var places = result.Value;
        places.Count.ShouldBe(1);
        var place = places[0];
        place.Name.ShouldBe("Place name");
        place.GoogleMapsUrl.ShouldBe("https://maps.google.com/1");
        place.Address.ShouldBe("Address");
        place.CountryCode.ShouldBe(Alpha2Code.PL);
        place.Longitude.ShouldBe(21.0);
        place.Latitude.ShouldBe(52.0);
        place.Comment.ShouldBe("Nice place");
    }

    private static Stream CreateJsonStream<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }
}