using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

using Nager.Country;

using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Tagging.Messages;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Infrastructure;

using Shouldly;

using Wolverine;

namespace Pinventory.Pins.Application.UnitTests.Importing;

public class ImportProcessingHandlerTests
{
    [Test]
    public async Task ProcessPinsBatch_creates_updates_conflicts_and_publishes_tagging()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, policyMock) = await CreateHandlerAsync();

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

        var removedComment = "No location information is available for this saved place"; // matches handler constant
        var places = new[]
        {
            new StarredPlace("SameName", "http://maps.google.com/?cid=222", "Addr 1", Alpha2Code.PL, 1, 2, DateTimeOffset.UtcNow,
                null), // conflict by name
            new StarredPlace("NewName", "http://maps.google.com/?cid=333", "Addr 2", Alpha2Code.PL, 3, 4, DateTimeOffset.UtcNow,
                null), // update by placeId
            new StarredPlace("Created", "http://maps.google.com/?cid=444", "Addr 3", Alpha2Code.PL, 5, 6, DateTimeOffset.UtcNow,
                null), // create new
            new StarredPlace("Removed", "http://maps.google.com/?cid=555", "Addr 4", Alpha2Code.PL, 7, 8, DateTimeOffset.UtcNow,
                removedComment) // failed
        };
        var message = new ProcessPinsBatchMessage(userId, archiveJobId, places);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        var reloadedImport = await dbContext.Imports.SingleAsync(i => i.UserId == userId);
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
        var (handler, dbContext, busMock, policyMock) = await CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        // Ensure TryComplete will fail by setting Total higher than the number of processed items
        import.SetTotal(100);
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
            new StarredPlace("SameName", "http://maps.google.com/?cid=222", "Addr 1", Alpha2Code.PL, 1, 2, DateTimeOffset.UtcNow,
                null), // conflict by name
            new StarredPlace("NewName", "http://maps.google.com/?cid=333", "Addr 2", Alpha2Code.PL, 3, 4, DateTimeOffset.UtcNow,
                null), // update by placeId
            new StarredPlace("Created", "http://maps.google.com/?cid=444", "Addr 3", Alpha2Code.PL, 5, 6, DateTimeOffset.UtcNow,
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
    public async Task ProcessPinsBatch_does_nothing_when_running_import_not_found()
    {
        // Arrange
        var (handler, _, busMock, _) = await CreateHandlerAsync();
        var places = new[]
        {
            new StarredPlace("Name", "http://maps.google.com/?cid=111", "Addr", Alpha2Code.PL, 1, 2, DateTimeOffset.UtcNow, null)
        };
        var message = new ProcessPinsBatchMessage("user-1", "job-404", places);

        // Act
        await handler.HandleAsync(message);

        // Assert
        busMock.Invocations.Count.ShouldBe(0);
    }

    private static async Task<(ImportProcessingHandler handler, PinsDbContext dbContext, Mock<IMessageContext> busMock,
        Mock<IImportConcurrencyPolicy>
        concurrencyPolicyMock)> CreateHandlerAsync()
    {
        var options = new DbContextOptionsBuilder<PinsDbContext>()
            .UseSqlite(connectionString: "Data Source=:memory:")
            .Options;

        var dbContext = new PinsDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var logger = Mock.Of<ILogger<ImportProcessingHandler>>();
        var busMock = new Mock<IMessageContext>();
        var concurrencyPolicyMock = new Mock<IImportConcurrencyPolicy>();

        // sensible defaults
        concurrencyPolicyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ImportProcessingHandler(logger, dbContext, busMock.Object);

        return (handler, dbContext, busMock, concurrencyPolicyMock);
    }
}