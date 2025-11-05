using FluentResults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

using Nager.Country;

using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Application.Importing.Services.Archive;
using Pinventory.Pins.Application.Tagging.Messages;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Infrastructure;

using Shouldly;

using Wolverine;

namespace Pinventory.Pins.Application.UnitTests;

public class ImportHandlerTests
{
    [Test]
    public async Task StartImport_creates_job_and_publishes_check_message()
    {
        // Arrange
        var userId = "user-1";
        var period = Period.AllTime;
        var archiveJobId = "job-123";

        var (handler, dbContext, busMock, _, _, _, _) = await CreateHandlerAsync();

        var command = new StartImportCommand(userId, period);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(archiveJobId);

        await dbContext.SaveChangesAsync();
        var import = await dbContext.Imports.FirstOrDefaultAsync(i => i.UserId == userId);
        import.ShouldNotBeNull();
        import.ArchiveJobId.ShouldBe(archiveJobId);
        import.State.ShouldBe(ImportState.InProgress);

        busMock.Invocations.Count.ShouldBe(2);
        busMock.Invocations.Any(i => i.Arguments[0] is ImportStarted).ShouldBeTrue();
        busMock.Invocations.Any(i => i.Arguments[0] is CheckJobMessage).ShouldBeTrue();
    }

    [Test]
    public async Task StartImport_fails_when_concurrent_import_exists()
    {
        // Arrange
        var userId = "user-1";
        var (handler, dbContext, busMock, _, _, policyMock, _) = await CreateHandlerAsync();

        policyMock.Setup(p => p.CanStartImportAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new StartImportCommand(userId, Period.AllTime);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        await dbContext.SaveChangesAsync();
        result.IsFailed.ShouldBeTrue();
        (await dbContext.Imports.CountAsync()).ShouldBe(0);
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task CancelImport_cancels_import_and_publishes_event()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, _, policyMock, _) = await CreateHandlerAsync();

        // Seed running import
        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear(); // detach to avoid carrying previous domain events

        var command = new CancelImportCommand(userId, archiveJobId);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<ImportCancelled>();
    }

    [Test]
    public async Task CancelImport_fails_when_running_import_not_found()
    {
        // Arrange
        var (handler, _, busMock, _, _, _, _) = await CreateHandlerAsync();

        var command = new CancelImportCommand("user-1", "job-404");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not found"));
        busMock.Invocations.Count.ShouldBe(0);
    }

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
            .ReturnsAsync((ImportState.InProgress, []));

        var message = new CheckJobMessage(userId, archiveJobId);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Method.Name.ShouldBe(nameof(IMessageContext.ReScheduleCurrentAsync));
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
            .ReturnsAsync((ImportState.Complete, [new Uri("https://a"), new Uri("https://b")]));

        var message = new CheckJobMessage(userId, archiveJobId);

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
            .ReturnsAsync((ImportState.Failed, []));

        var message = new CheckJobMessage(userId, archiveJobId);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<ImportFailed>();
    }

    [Test]
    public async Task DownloadArchive_does_nothing_when_urls_missing()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, _, policyMock, _) = await CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();

        var message = new DownloadArchiveMessage(userId, archiveJobId, ["https://only-one"]);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task DownloadArchive_publishes_batches_for_returned_features()
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

        var features = new[]
        {
            new Feature(new Geometry([1.0, 2.0], "Point"),
                new Properties(DateTimeOffset.UtcNow, "https://maps.google.com/?cid=111",
                    new LocationAndName("Addr 1", Alpha2Code.PL, "Name 1"), null), "Feature"),
            new Feature(new Geometry([3.0, 4.0], "Point"),
                new Properties(DateTimeOffset.UtcNow, "https://maps.google.com/?cid=222",
                    new LocationAndName("Addr 2", Alpha2Code.PL, "Name 2"), null), "Feature"),
            new Feature(new Geometry([5.0, 6.0], "Point"),
                new Properties(DateTimeOffset.UtcNow, "https://maps.google.com/?cid=333",
                    new LocationAndName("Addr 3", Alpha2Code.PL, "Name 3"), null), "Feature")
        };
        var data = new SavedPlacesCollection("FeatureCollection", features);
        downloaderMock.Setup(d => d.DownloadAsync(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(((new ArchiveBrowser("now", "0", [])), data)));

        var message = new DownloadArchiveMessage(userId, archiveJobId, ["https://a", "https://b"]);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(1);
        var published = busMock.Invocations[0].Arguments[0].ShouldBeOfType<ProcessPinsBatchMessage>();
        published.StarredPlaces.Count().ShouldBe(features.Length);
    }

    [Test]
    public async Task ProcessPinsBatch_creates_updates_conflicts_and_publishes_tagging()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, _, policyMock, _) = await CreateHandlerAsync();


        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        await dbContext.Imports.AddAsync(import);

        // existing pins: one for conflict by name, one to update by place id
        var address = new Address("Addr X", Alpha2Code.PL);
        var location = new Location(10, 20);
        var conflictPin = new Pin(userId, "SameName", new GooglePlaceId("111"), address, location, DateTimeOffset.UtcNow);
        var updatePin = new Pin(userId, "OldName", new GooglePlaceId("333"), address, location, DateTimeOffset.UtcNow);
        await dbContext.Pins.AddRangeAsync(conflictPin, updatePin);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var removedComment = "No location information is available for this saved place"; // matches ImportHandler constant
        var places = new[]
        {
            new StarredPlace("SameName", "https://maps.google.com/?cid=222", "Addr 1", Alpha2Code.PL, 1, 2, DateTimeOffset.UtcNow,
                null), // conflict by name
            new StarredPlace("NewName", "https://maps.google.com/?cid=333", "Addr 2", Alpha2Code.PL, 3, 4, DateTimeOffset.UtcNow,
                null), // update by placeId
            new StarredPlace("Created", "https://maps.google.com/?cid=444", "Addr 3", Alpha2Code.PL, 5, 6, DateTimeOffset.UtcNow,
                null), // create new
            new StarredPlace("Removed", "https://maps.google.com/?cid=555", "Addr 4", Alpha2Code.PL, 7, 8, DateTimeOffset.UtcNow,
                removedComment) // failed
        };
        var message = new ProcessPinsBatchMessage(userId, archiveJobId, places);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        var reloadedImport = await dbContext.Imports.FirstAsync(i => i.UserId == userId);
        reloadedImport.State.ShouldBe(ImportState.Complete);
        reloadedImport.Processed.ShouldBe(4);
        reloadedImport.Created.ShouldBe(1);
        reloadedImport.Updated.ShouldBe(1);
        reloadedImport.Failed.ShouldBe(1);
        reloadedImport.Conflicts.ShouldBe(1);

        // Two pins should be tagged (created + updated)
        var publishCalls = busMock.Invocations.Where(i => i.Arguments.Count > 0 && i.Arguments[0] is AssignTagsToPinMessage).ToList();
        publishCalls.Count.ShouldBe(2);
    }

    [Test]
    public async Task ProcessPinsBatch_raises_events_adds_created_pins_and_publishes_tagging_when_job_not_yet_finished()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, _, policyMock, _) = await CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        // Ensure TryComplete will fail by setting Total higher than the number of processed items
        import.UpdateTotal(100);
        await dbContext.Imports.AddAsync(import);

        // existing pins: one for conflict by name, one to update by place id
        var address = new Address("Addr X", Alpha2Code.PL);
        var location = new Location(10, 20);
        var conflictPin = new Pin(userId, "SameName", new GooglePlaceId("111"), address, location, DateTimeOffset.UtcNow);
        var updatePin = new Pin(userId, "OldName", new GooglePlaceId("333"), address, location, DateTimeOffset.UtcNow);
        await dbContext.Pins.AddRangeAsync(conflictPin, updatePin);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var places = new[]
        {
            new StarredPlace("SameName", "https://maps.google.com/?cid=222", "Addr 1", Alpha2Code.PL, 1, 2, DateTimeOffset.UtcNow,
                null), // conflict by name
            new StarredPlace("NewName", "https://maps.google.com/?cid=333", "Addr 2", Alpha2Code.PL, 3, 4, DateTimeOffset.UtcNow,
                null), // update by placeId
            new StarredPlace("Created", "https://maps.google.com/?cid=444", "Addr 3", Alpha2Code.PL, 5, 6, DateTimeOffset.UtcNow,
                null) // create new
        };
        var message = new ProcessPinsBatchMessage(userId, archiveJobId, places);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        var reloadedImport = await dbContext.Imports.FirstAsync(i => i.UserId == userId);
        reloadedImport.State.ShouldBe(ImportState.InProgress);
        busMock.Invocations.Any(i => i.Arguments.Count > 0 && i.Arguments[0] is ImportBatchProcessed).ShouldBeTrue();
        dbContext.Pins.Local.Any(p => p.Name == "Created").ShouldBeTrue();
        var tagPublishCalls = busMock.Invocations.Where(i => i.Arguments.Count > 0 && i.Arguments[0] is AssignTagsToPinMessage).ToList();
        tagPublishCalls.Count.ShouldBe(2);
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
            .ReturnsAsync((ImportState.Cancelled, []));

        var message = new CheckJobMessage(userId, archiveJobId);

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

        var message = new CheckJobMessage(userId, archiveJobId);

        // Act
        await handler.HandleAsync(message);

        // Assert
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task DownloadArchive_does_nothing_when_running_import_not_found()
    {
        // Arrange
        var (handler, _, busMock, _, _, _, _) = await CreateHandlerAsync();
        var message = new DownloadArchiveMessage("user-1", "job-404", new List<string> { "https://a", "https://b" });

        // Act
        await handler.HandleAsync(message);

        // Assert
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task DownloadArchive_does_nothing_when_downloader_fails()
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

        downloaderMock.Setup(d => d.DownloadAsync(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("download failed"));

        var message = new DownloadArchiveMessage(userId, archiveJobId, new List<string> { "https://a", "https://b" });

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task ProcessPinsBatch_does_nothing_when_running_import_not_found()
    {
        // Arrange
        var (handler, _, busMock, _, _, _, _) = await CreateHandlerAsync();
        var places = new[]
        {
            new StarredPlace("Name", "https://maps.google.com/?cid=111", "Addr", Alpha2Code.PL, 1, 2, DateTimeOffset.UtcNow, null)
        };
        var message = new ProcessPinsBatchMessage("user-1", "job-404", places);

        // Act
        await handler.HandleAsync(message);

        // Assert
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task StartImport_throws_when_service_factory_fails()
    {
        // Arrange
        var userId = "user-1";
        var (handler, dbContext, busMock, factoryMock, _, _, _) = await CreateHandlerAsync();

        factoryMock.Setup(f => f.CreateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<IImportService>("factory failed"));

        var command = new StartImportCommand(userId, Period.AllTime);

        // Act + Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await handler.HandleAsync(command));
        (await dbContext.Imports.CountAsync()).ShouldBe(0);
        busMock.Invocations.Count.ShouldBe(0);
    }

    private static async Task<(ImportHandler handler, PinsDbContext dbContext, Mock<IMessageContext> busMock, Mock<IImportServiceFactory>
        factoryMock, Mock<IImportService> serviceMock, Mock<IImportConcurrencyPolicy> concurrencyPolicyMock, Mock<IArchiveDownloader>
        downloaderMock)> CreateHandlerAsync()
    {
        var options = new DbContextOptionsBuilder<PinsDbContext>()
            .UseSqlite(connectionString: "Data Source=:memory:")
            .Options;

        var dbContext = new PinsDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var logger = Mock.Of<ILogger<ImportHandler>>();
        var busMock = new Mock<IMessageContext>();
        var factoryMock = new Mock<IImportServiceFactory>();
        var serviceMock = new Mock<IImportService>();
        var concurrencyPolicyMock = new Mock<IImportConcurrencyPolicy>();
        var downloaderMock = new Mock<IArchiveDownloader>();

        // sensible defaults
        serviceMock.Setup(s => s.InitiateAsync(It.IsAny<Period?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("job-123");
        factoryMock.Setup(f => f.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(serviceMock.Object));
        concurrencyPolicyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ImportHandler(logger, factoryMock.Object, dbContext, busMock.Object, concurrencyPolicyMock.Object,
            downloaderMock.Object);

        return (handler, dbContext, busMock, factoryMock, serviceMock, concurrencyPolicyMock, downloaderMock);
    }
}