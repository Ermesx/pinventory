using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;

using Moq;

using Pinventory.Pins.Infrastructure.Services.Downloading;

using Shouldly;

namespace Pinventory.Pins.Infrastructure.UnitTests.Services.Downloading;

public class HttpZipDownloaderTests
{
    [Test]
    public async Task DownloadAsync_returns_stream_when_zip_contains_requested_entry()
    {
        // Arrange
        var filePath = "folder/file.txt";
        var zipBytes = CreateZipWithEntry(filePath, "hello world");

        var downloader = CreateDownloader((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(zipBytes)) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            return response;
        });

        // Act
        var stream = await downloader.DownloadAsync(new Uri("https://example.com/archive.zip"), filePath);

        // Assert
        stream.ShouldNotBeNull();
    }

    [Test]
    public async Task DownloadAsync_throws_when_content_type_is_not_zip()
    {
        // Arrange
        var filePath = "folder/file.txt";
        var zipBytes = CreateZipWithEntry(filePath, "hello world");

        var downloader = CreateDownloader((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(zipBytes)) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return response;
        });

        // Act & Assert
        await Should.ThrowAsync<InvalidDataException>(async () =>
            await downloader.DownloadAsync(new Uri("https://example.com/archive.zip"), filePath));
    }

    [Test]
    public async Task DownloadAsync_throws_when_http_response_is_unsuccessful()
    {
        // Arrange
        var filePath = "folder/file.txt";

        var downloader = CreateDownloader((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StreamContent(new MemoryStream()) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            return response;
        });

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(async () =>
            await downloader.DownloadAsync(new Uri("https://example.com/archive.zip"), filePath));
    }

    [Test]
    public async Task DownloadAsync_throws_when_entry_is_missing_from_zip()
    {
        // Arrange
        var existingPath = "folder/existing.txt";
        var missingPath = "folder/missing.txt";
        var zipBytes = CreateZipWithEntry(existingPath, "hello world");

        var downloader = CreateDownloader((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(zipBytes)) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            return response;
        });

        // Act & Assert
        await Should.ThrowAsync<FileNotFoundException>(async () =>
            await downloader.DownloadAsync(new Uri("https://example.com/archive.zip"), missingPath));
    }

    [Test]
    public async Task DownloadAsync_throws_when_zip_is_malformed()
    {
        // Arrange
        var filePath = "folder/file.txt";
        var malformedBytes = new byte[] { 1, 2, 3, 4, 5 };

        var downloader = CreateDownloader((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(malformedBytes)) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            return response;
        });

        // Act & Assert
        await Should.ThrowAsync<InvalidDataException>(async () =>
            await downloader.DownloadAsync(new Uri("https://example.com/archive.zip"), filePath));
    }

    private static HttpZipDownloader CreateDownloader(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory)
    {
        var handler = new DelegatingHandlerStub(responseFactory);
        var httpClient = new HttpClient(handler);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new HttpZipDownloader(factoryMock.Object);
    }

    private static byte[] CreateZipWithEntry(string entryPath, string content)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryPath);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            writer.Write(content);
        }

        return memoryStream.ToArray();
    }

    private sealed class DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request, cancellationToken));
        }
    }
}