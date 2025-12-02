using FluentResults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

using Nager.Country;

using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Infrastructure;
using Pinventory.Pins.Infrastructure.Sagas.Messages;

using Shouldly;

using Wolverine;

namespace Pinventory.Pins.Application.UnitTests.Importing;

public class ImportDownloadHandlerTests
{
    [Test]
    public async Task CheckJob_reschedules_when_archive_in_progress()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, serviceMock, policyMock, _) = await CreateHandlerAsync();

        // Seed running import
        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        serviceMock.Setup(s => s.CheckJobAsync(archiveJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(ImportState State, IEnumerable<Uri> Urls)>((ImportState.InProgress, [])));

        var message = new CheckJobMessage(import.Id, userId, archiveJobId);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<CheckJobMessage>();
    }

    [Test]
    public async Task CheckJob_publishes_download_message_when_archive_complete()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, serviceMock, policyMock, _) = await CreateHandlerAsync();

        // Seed running import
        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        serviceMock.Setup(s => s.CheckJobAsync(archiveJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(ImportState State, IEnumerable<Uri> Urls)>((ImportState.Complete, [
                new Uri("https://a"), new Uri("https://b")
            ])));

        var message = new CheckJobMessage(import.Id, userId, archiveJobId);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<DownloadArchiveMessage>();
    }

    [Test]
    public async Task CheckJob_marks_import_failed_when_archive_failed()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, serviceMock, policyMock, _) = await CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        serviceMock.Setup(s => s.CheckJobAsync(archiveJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(ImportState State, IEnumerable<Uri> Urls)>((ImportState.Failed, [])));

        var message = new CheckJobMessage(import.Id, userId, archiveJobId);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<ImportFailed>();
    }

    [Test]
    public async Task CheckJob_marks_import_cancelled_when_archive_cancelled()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, serviceMock, policyMock, _) = await CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        serviceMock.Setup(s => s.CheckJobAsync(archiveJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<(ImportState State, IEnumerable<Uri> Urls)>((ImportState.Cancelled, [])));

        var message = new CheckJobMessage(import.Id, userId, archiveJobId);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<ImportCancelled>();
    }

    [Test]
    public async Task CheckJob_does_nothing_when_running_import_not_found()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-404";
        var (handler, _, busMock, _, _, _, _) = await CreateHandlerAsync();

        var message = new CheckJobMessage(Guid.NewGuid(), userId, archiveJobId);

        // Act
        await handler.HandleAsync(message);

        // Assert
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task DownloadArchive_fails_when_urls_missing()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, _, policyMock, downloaderMock) = await CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var urls = new List<string> { "https://only-one" };
        downloaderMock.Setup(p => p.ProvideAsync(It.Is<IReadOnlyList<Uri>>(u => u.Count == urls.Count), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("not enough urls"));

        var message = new DownloadArchiveMessage(import.Id, userId, archiveJobId, urls);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<ImportFailed>();
    }

    [Test]
    public async Task DownloadArchive_publishes_places_for_returned_places()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, _, policyMock, downloaderMock) = await CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var places = new List<StarredPlace>
        {
            new("Name 1", "http://maps.google.com/?cid=111", "Addr 1", Alpha2Code.PL, 1.0, 2.0, DateTimeOffset.UtcNow, null),
            new("Name 2", "http://maps.google.com/?cid=222", "Addr 2", Alpha2Code.PL, 3.0, 4.0, DateTimeOffset.UtcNow, null),
            new("Name 3", "http://maps.google.com/?cid=333", "Addr 3", Alpha2Code.PL, 5.0, 6.0, DateTimeOffset.UtcNow, null)
        };

        downloaderMock.Setup(p => p.ProvideAsync(It.IsAny<IReadOnlyList<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<IReadOnlyList<StarredPlace>>(places));

        var message = new DownloadArchiveMessage(import.Id, userId, archiveJobId, ["https://a", "https://b"]);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(4);
        var published = busMock.Invocations[0].Arguments[0].ShouldBeOfType<ImportPlaceRegistered>();
        published.UserId.ShouldBe(userId);
        published.ArchiveJobId.ShouldBe(archiveJobId);
        busMock.Invocations[1].Arguments[0].ShouldBeOfType<ImportPlaceRegistered>();
        busMock.Invocations[2].Arguments[0].ShouldBeOfType<ImportPlaceRegistered>();
        busMock.Invocations[3].Arguments[0].ShouldBeOfType<ExpectedBatchesMessage>();
    }

    [Test]
    public async Task DownloadArchive_does_nothing_when_running_import_not_found()
    {
        // Arrange
        var (handler, _, busMock, _, _, _, _) = await CreateHandlerAsync();
        var message = new DownloadArchiveMessage(Guid.NewGuid(), "user-1", "job-404", new List<string> { "https://a", "https://b" });

        // Act
        await handler.HandleAsync(message);

        // Assert
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task DownloadArchive_does_nothing_when_provider_fails()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, _, policyMock, downloaderMock) = await CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        downloaderMock.Setup(p => p.ProvideAsync(It.IsAny<IReadOnlyList<Uri>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("download failed"));

        var message = new DownloadArchiveMessage(import.Id, userId, archiveJobId, new List<string> { "https://a", "https://b" });

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<ImportFailed>();
    }

    private static async Task<(ImportDownloadHandler handler, PinsDbContext dbContext, Mock<IMessageContext> busMock,
        Mock<IImportServiceFactory>
        factoryMock, Mock<IImportService> serviceMock, Mock<IImportConcurrencyPolicy> concurrencyPolicyMock, Mock<IStarredPlacesProvider>
        downloaderMock)> CreateHandlerAsync()
    {
        var options = new DbContextOptionsBuilder<PinsDbContext>()
            .UseSqlite(connectionString: "Data Source=:memory:")
            .Options;

        var dbContext = new PinsDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var logger = Mock.Of<ILogger<ImportDownloadHandler>>();
        var busMock = new Mock<IMessageContext>();
        var factoryMock = new Mock<IImportServiceFactory>();
        var serviceMock = new Mock<IImportService>();
        var concurrencyPolicyMock = new Mock<IImportConcurrencyPolicy>();
        var downloaderMock = new Mock<IStarredPlacesProvider>();

        // sensible defaults
        factoryMock.Setup(f => f.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(serviceMock.Object));
        concurrencyPolicyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ImportDownloadHandler(logger, factoryMock.Object, dbContext, busMock.Object, downloaderMock.Object);

        return (handler, dbContext, busMock, factoryMock, serviceMock, concurrencyPolicyMock, downloaderMock);
    }
}