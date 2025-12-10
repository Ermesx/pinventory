using System.Net;
using System.Text;
using System.Text.Json;

using FluentResults;

using Moq;

using Pinventory.Pins.Import.Worker.DataPortability.Archive;
using Pinventory.Pins.Import.Worker.DataPortability.Archive.Dtos;
using Pinventory.Pins.Infrastructure.Downloading;

using Shouldly;

namespace Pinventory.Pins.Import.Worker.UnitTests.DataPortability.Archive;

public class GoogleArchiveProcessorTests
{
    [Test]
    public async Task GetArchiveAsync_returns_metadata_and_data_when_all_ok()
    {
        // Arrange
        var archiveBrowserUri = new Uri("https://example.com/archive_browser.zip");
        var dataFilesUri = new Uri("https://example.com/data_files.zip");

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

        var savedPlaces = new SavedPlacesCollection("FeatureCollection", []);

        await using var archiveBrowserStream = CreateJsonStream(archiveBrowser);
        await using var savedPlacesStream = CreateJsonStream(savedPlaces);

        var downloaderMock = new Mock<IZipDownloader>();
        downloaderMock
            .Setup(d => d.DownloadAsync(archiveBrowserUri, "Portability/archive_browser.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(archiveBrowserStream);
        downloaderMock
            .Setup(d => d.DownloadAsync(dataFilesUri, "Portability/folder/saved_places.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedPlacesStream);

        var sut = new GoogleArchiveProcessor(downloaderMock.Object);

        // Act
        var result = await sut.GetArchiveAsync(archiveBrowserUri, dataFilesUri, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var (metadata, data) = result.Value;
        metadata.ServiceStatus.ShouldHaveSingleItem();
        data.Features.ShouldBeEmpty();
    }

    [Test]
    public async Task GetArchiveAsync_returns_failure_when_no_service_status()
    {
        // Arrange
        var archiveBrowserUri = new Uri("https://example.com/archive_browser.zip");
        var dataFilesUri = new Uri("https://example.com/data_files.zip");

        var archiveBrowser = new ArchiveBrowser(
            "2024-01-01T00:00:00Z",
            "100MB",
            []);

        await using var archiveBrowserStream = CreateJsonStream(archiveBrowser);

        var downloaderMock = new Mock<IZipDownloader>();
        downloaderMock
            .Setup(d => d.DownloadAsync(archiveBrowserUri, "Portability/archive_browser.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(archiveBrowserStream);

        var sut = new GoogleArchiveProcessor(downloaderMock.Object);

        // Act
        var result = await sut.GetArchiveAsync(archiveBrowserUri, dataFilesUri, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Missing service in archive browser"));
    }

    [Test]
    public async Task GetArchiveAsync_returns_failure_when_no_extracted_file_metadata()
    {
        // Arrange
        var archiveBrowserUri = new Uri("https://example.com/archive_browser.zip");
        var dataFilesUri = new Uri("https://example.com/data_files.zip");

        var archiveBrowser = new ArchiveBrowser(
            "2024-01-01T00:00:00Z",
            "100MB",
            [
                new ServiceStatus(
                    new Service("Maps"),
                    "Google Maps",
                    false,
                    [],
                    "folder",
                    new ServiceInformation(
                        "Maps",
                        new BriefDescription("desc"),
                        new PromoText("promo"),
                        new FolderStructure("structure"),
                        [],
                        []),
                    "0",
                    "0",
                    "0")
            ]);

        await using var archiveBrowserStream = CreateJsonStream(archiveBrowser);

        var downloaderMock = new Mock<IZipDownloader>();
        downloaderMock
            .Setup(d => d.DownloadAsync(archiveBrowserUri, "Portability/archive_browser.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(archiveBrowserStream);

        var sut = new GoogleArchiveProcessor(downloaderMock.Object);

        // Act
        var result = await sut.GetArchiveAsync(archiveBrowserUri, dataFilesUri, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Missing metadata for extracted file"));
    }

    [Test]
    public async Task GetArchiveAsync_propagates_failures_from_archive_download()
    {
        // Arrange
        var archiveBrowserUri = new Uri("https://example.com/archive_browser.zip");
        var dataFilesUri = new Uri("https://example.com/data_files.zip");

        Result.Fail<(ArchiveBrowser, SavedPlacesCollection)>("download failed");

        var downloaderMock = new Mock<IZipDownloader>();
        downloaderMock
            .Setup(d => d.DownloadAsync(archiveBrowserUri, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("error", null, HttpStatusCode.BadRequest));

        var sut = new GoogleArchiveProcessor(downloaderMock.Object);

        // Act
        var result = await sut.GetArchiveAsync(archiveBrowserUri, dataFilesUri, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    private static Stream CreateJsonStream<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }
}