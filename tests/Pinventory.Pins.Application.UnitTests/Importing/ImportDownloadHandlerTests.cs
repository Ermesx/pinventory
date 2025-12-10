using FluentResults;

using Microsoft.Extensions.Logging;

using Moq;

using Nager.Country;

using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;

using Shouldly;

namespace Pinventory.Pins.Application.UnitTests.Importing;

public class ImportDownloadHandlerTests
{
    [Test]
    public async Task CheckJob_reschedules_when_archive_in_progress()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, _, serviceMock, policyMock, _) = CreateHandlerAsync();

        // Seed running import
        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        serviceMock.Setup(s => s.CheckJobAsync(archiveJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(ImportState State, IEnumerable<Uri> Urls)>((ImportState.InProgress, [])));

        var message = new CheckJobMessage(import.Id, userId, archiveJobId);

        // Act
        var response = await handler.HandleAsync(message, import);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        response.CheckJobMessage.ShouldNotBeNull();
        response.DownloadMessage.ShouldBeNull();
    }

    [Test]
    public async Task CheckJob_publishes_download_message_when_archive_complete()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, _, serviceMock, policyMock, _) = CreateHandlerAsync();

        // Seed running import
        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        serviceMock.Setup(s => s.CheckJobAsync(archiveJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(ImportState State, IEnumerable<Uri> Urls)>((ImportState.Complete, [
                new Uri("https://a"), new Uri("https://b")
            ])));

        var message = new CheckJobMessage(import.Id, userId, archiveJobId);

        // Act
        var response = await handler.HandleAsync(message, import);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        response.DownloadMessage.ShouldNotBeNull();
        response.CheckJobMessage.ShouldBeNull();
    }

    [Test]
    public async Task CheckJob_marks_import_failed_when_archive_failed()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, _, serviceMock, policyMock, _) = CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        serviceMock.Setup(s => s.CheckJobAsync(archiveJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(ImportState State, IEnumerable<Uri> Urls)>((ImportState.Failed, [])));

        var message = new CheckJobMessage(import.Id, userId, archiveJobId);

        // Act
        var response = await handler.HandleAsync(message, import);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        import.State.ShouldBe(ImportState.Failed);
        response.CheckJobMessage.ShouldBeNull();
        response.DownloadMessage.ShouldBeNull();
    }

    [Test]
    public async Task CheckJob_marks_import_cancelled_when_archive_cancelled()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, _, serviceMock, policyMock, _) = CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        serviceMock.Setup(s => s.CheckJobAsync(archiveJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(ImportState State, IEnumerable<Uri> Urls)>((ImportState.Cancelled, [])));

        var message = new CheckJobMessage(import.Id, userId, archiveJobId);

        // Act
        var response = await handler.HandleAsync(message, import);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        import.State.ShouldBe(ImportState.Cancelled);
        response.CheckJobMessage.ShouldBeNull();
        response.DownloadMessage.ShouldBeNull();
    }

    [Test]
    public async Task CheckJob_does_nothing_when_running_import_not_found()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-404";
        var (handler, _, _, _, _) = CreateHandlerAsync();

        var message = new CheckJobMessage(Guid.NewGuid(), userId, archiveJobId);

        // Act
        var response = await handler.HandleAsync(message, null);

        // Assert
        response.CheckJobMessage.ShouldBeNull();
        response.DownloadMessage.ShouldBeNull();
    }

    [Test]
    public async Task DownloadArchive_fails_when_urls_missing()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, _, _, policyMock, downloaderMock) = CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        var urls = new List<string> { "https://only-one" };
        downloaderMock.Setup(p => p.ProvideAsync(It.Is<IReadOnlyList<Uri>>(u => u.Count == urls.Count), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("not enough urls"));

        var message = new DownloadArchiveMessage(import.Id, userId, archiveJobId, urls);

        // Act
        var responseMessage = await handler.HandleAsync(message, import);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        responseMessage.ShouldBeNull();
    }

    [Test]
    public async Task DownloadArchive_publishes_places_for_returned_places()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, _, _, policyMock, downloaderMock) = CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        var places = new List<StarredPlace>
        {
            new("Name 1", "http://maps.google.com/?cid=111", "Addr 1", Alpha2Code.PL, 1.0, 2.0, DateTimeOffset.UtcNow.AddDays(-1),
                null),
            new("Name 2", "http://maps.google.com/?cid=222", "Addr 2", Alpha2Code.PL, 3.0, 4.0, DateTimeOffset.UtcNow.AddDays(-1),
                null),
            new("Name 3", "http://maps.google.com/?cid=333", "Addr 3", Alpha2Code.PL, 5.0, 6.0, DateTimeOffset.UtcNow.AddDays(-1), null)
        };

        downloaderMock.Setup(p => p.ProvideAsync(It.IsAny<IReadOnlyList<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<IReadOnlyList<StarredPlace>>(places));

        var message = new DownloadArchiveMessage(import.Id, userId, archiveJobId, ["https://a", "https://b"]);

        // Act
        var responseMessage = await handler.HandleAsync(message, import);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        responseMessage.ShouldNotBeNull();
        responseMessage.BatchesCount.ShouldBe(1);
        responseMessage.Id.ShouldBe(import.Id);
        responseMessage.UserId.ShouldBe(userId);
        responseMessage.ArchiveJobId.ShouldBe(archiveJobId);
    }

    [Test]
    public async Task DownloadArchive_does_nothing_when_running_import_not_found()
    {
        // Arrange
        var (handler, _, _, _, _) = CreateHandlerAsync();
        var message = new DownloadArchiveMessage(Guid.NewGuid(), "user-1", "job-404", new List<string> { "https://a", "https://b" });

        // Act
        var responseMessage = await handler.HandleAsync(message, null);

        // Assert
        responseMessage.ShouldBeNull();
    }

    [Test]
    public async Task DownloadArchive_does_nothing_when_provider_fails()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, _, _, policyMock, downloaderMock) = CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        downloaderMock.Setup(p => p.ProvideAsync(It.IsAny<IReadOnlyList<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("download failed"));

        var message = new DownloadArchiveMessage(import.Id, userId, archiveJobId, new List<string> { "https://a", "https://b" });

        // Act
        var responseMessage = await handler.HandleAsync(message, import);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        responseMessage.ShouldBeNull();
    }

    private static (ImportDownloadHandler handler,
        Mock<IImportServiceFactory>
        factoryMock, Mock<IImportService> serviceMock, Mock<IImportConcurrencyPolicy> concurrencyPolicyMock, Mock<IStarredPlacesProvider>
        downloaderMock) CreateHandlerAsync()
    {
        var logger = Mock.Of<ILogger<ImportDownloadHandler>>();
        var factoryMock = new Mock<IImportServiceFactory>();
        var serviceMock = new Mock<IImportService>();
        var concurrencyPolicyMock = new Mock<IImportConcurrencyPolicy>();
        var downloaderMock = new Mock<IStarredPlacesProvider>();

        // sensible defaults
        factoryMock.Setup(f => f.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(serviceMock.Object));
        concurrencyPolicyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ImportDownloadHandler(logger, factoryMock.Object, downloaderMock.Object);

        return (handler, factoryMock, serviceMock, concurrencyPolicyMock, downloaderMock);
    }
}