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
using Pinventory.Pins.Infrastructure.Sagas.Messages;

using Shouldly;

using Wolverine;

namespace Pinventory.Pins.Application.UnitTests.Importing;

public class ImportProcessingHandlerTests
{
    [Test]
    public async Task ProcessPlaces_creates_updates_conflicts_and_publishes_tagging()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, policyMock, validatorMock) = await CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        // existing pins: one for conflict by name, one to update by place id
        var address = new Address("Addr X", Alpha2Code.PL);
        var location = new Location(10, 20);
        var conflictPin = new Pin(userId, "SameName", new GooglePlaceId("111"), address, location, DateTimeOffset.UtcNow);
        var updatePin = new Pin(userId, "OldName", new GooglePlaceId("333"), address, location, DateTimeOffset.UtcNow);
        await dbContext.Pins.AddRangeAsync(conflictPin, updatePin);

        var places = new[]
        {
            new StarredPlace("SameName", "http://maps.google.com/?cid=222", "Addr 1", Alpha2Code.PL, 1, 2, DateTimeOffset.UtcNow,
                null), // conflict by name
            new StarredPlace("NewName", "http://maps.google.com/?cid=333", "Addr 2", Alpha2Code.PL, 3, 4, DateTimeOffset.UtcNow,
                null), // update by placeId
            new StarredPlace("Created", "http://maps.google.com/?cid=444", "Addr 3", Alpha2Code.PL, 5, 6, DateTimeOffset.UtcNow,
                null), // create new
            new StarredPlace("Removed", "http://maps.google.com/?cid=555", "Addr 4", Alpha2Code.PL, 7, 8, DateTimeOffset.UtcNow,
                null) // failed
        };

        var registerResult = import.RegisterPlaces(places);
        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        // Set up a validator to return appropriate states
        validatorMock.Setup(v =>
                v.ValidateAsync(It.IsAny<Import>(), It.Is<StarredPlace>(p => p.Name == "SameName"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.Conflicted);
        validatorMock.Setup(v =>
                v.ValidateAsync(It.IsAny<Import>(), It.Is<StarredPlace>(p => p.Name == "NewName"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.Exists);
        validatorMock.Setup(v =>
                v.ValidateAsync(It.IsAny<Import>(), It.Is<StarredPlace>(p => p.Name == "Created"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);
        validatorMock.Setup(v =>
                v.ValidateAsync(It.IsAny<Import>(), It.Is<StarredPlace>(p => p.Name == "Removed"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.Invalid);

        var placeIds = import.StarredPlaces.Select(x => x.Id).ToArray();
        var message = new PlacesProcessingBatchMessage(import.Id, userId, archiveJobId, placeIds);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        registerResult.IsSuccess.ShouldBeTrue();
        var reloadedImport = await dbContext.Imports.SingleAsync(i => i.UserId == userId);
        reloadedImport.Processed.ShouldBe(4);
        reloadedImport.Created.ShouldBe(1);
        reloadedImport.Updated.ShouldBe(1);
        reloadedImport.Failed.ShouldBe(1);
        reloadedImport.Conflicts.ShouldBe(1);

        // Two pins should be tagged (created + updated)
        var publishCalls = busMock.Invocations.Where(i => i.Arguments.Count > 0 && i.Arguments[0] is AssignTagsToPinMessage).ToList();
        publishCalls.Count.ShouldBe(2);

        // ImportPlacesProcessed event should be published
        busMock.Invocations.Any(i => i.Arguments.Count > 0 && i.Arguments[0] is ImportPlaceProcessed).ShouldBeTrue();
    }

    [Test]
    public async Task ProcessPlaces_raises_events_adds_created_pins_and_publishes_tagging_when_job_not_yet_finished()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, policyMock, validatorMock) = await CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        // existing pins: one for conflict by name, one to update by place id
        var address = new Address("Addr X", Alpha2Code.PL);
        var location = new Location(10, 20);
        var conflictPin = new Pin(userId, "SameName", new GooglePlaceId("111"), address, location, DateTimeOffset.UtcNow);
        var updatePin = new Pin(userId, "OldName", new GooglePlaceId("333"), address, location, DateTimeOffset.UtcNow);
        await dbContext.Pins.AddRangeAsync(conflictPin, updatePin);

        var places = new[]
        {
            new StarredPlace("SameName", "http://maps.google.com/?cid=222", "Addr 1", Alpha2Code.PL, 1, 2, DateTimeOffset.UtcNow,
                null), // conflict by name
            new StarredPlace("NewName", "http://maps.google.com/?cid=333", "Addr 2", Alpha2Code.PL, 3, 4, DateTimeOffset.UtcNow,
                null), // update by placeId
            new StarredPlace("Created", "http://maps.google.com/?cid=444", "Addr 3", Alpha2Code.PL, 5, 6, DateTimeOffset.UtcNow,
                null) // create new
        };

        var registerResult = import.RegisterPlaces(places);
        // Add more places to ensure TryComplete will fail
        import.RegisterPlaces([
            new StarredPlace("Extra", "http://maps.google.com/?cid=999", "Addr", Alpha2Code.PL, 9, 10, DateTimeOffset.UtcNow, null)
        ]);

        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        // Set up a validator to return appropriate states
        validatorMock.Setup(v =>
                v.ValidateAsync(It.IsAny<Import>(), It.Is<StarredPlace>(p => p.Name == "SameName"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.Conflicted);
        validatorMock.Setup(v =>
                v.ValidateAsync(It.IsAny<Import>(), It.Is<StarredPlace>(p => p.Name == "NewName"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.Exists);
        validatorMock.Setup(v =>
                v.ValidateAsync(It.IsAny<Import>(), It.Is<StarredPlace>(p => p.Name == "Created"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);

        var placeIds = import.StarredPlaces.Select(x => x.Id).ToArray();
        var message = new PlacesProcessingBatchMessage(import.Id, userId, archiveJobId, placeIds);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        registerResult.IsSuccess.ShouldBeTrue();
        var reloadedImport = await dbContext.Imports.FirstAsync(i => i.UserId == userId);
        reloadedImport.State.ShouldBe(ImportState.InProgress);
        busMock.Invocations.Any(i => i.Arguments.Count > 0 && i.Arguments[0] is ImportPlaceProcessed).ShouldBeTrue();
        dbContext.Pins.Local.Any(p => p.Name == "Created").ShouldBeTrue();
        var tagPublishCalls = busMock.Invocations.Where(i => i.Arguments.Count > 0 && i.Arguments[0] is AssignTagsToPinMessage).ToList();
        tagPublishCalls.Count.ShouldBe(3);
    }

    [Test]
    public async Task ProcessPlaces_does_nothing_when_running_import_not_found()
    {
        // Arrange
        var (handler, _, busMock, _, _) = await CreateHandlerAsync();
        var message = new PlacesProcessingBatchMessage(Guid.NewGuid(), "user-1", "job-404", [Guid.NewGuid()]);

        // Act
        await handler.HandleAsync(message);

        // Assert
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task TryComplete_completes_import_when_all_places_processed()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, policyMock, validatorMock) = await CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        var places = new[]
        {
            new StarredPlace("Name", "http://maps.google.com/?cid=111", "Addr", Alpha2Code.PL, 1, 2, DateTimeOffset.UtcNow, null)
        };

        var registerResult = import.RegisterPlaces(places);

        var placeId = import.StarredPlaces.Single().Id;
        var placesToProcess = new HashSet<Guid> { placeId };
        var processResult = await import.ProcessPlacesAsync(placesToProcess, validatorMock.Object);

        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        // Set up a validator
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<Import>(), It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);

        // First process the place to set Processed count
        var processingMessage = new PlacesProcessingBatchMessage(import.Id, userId, archiveJobId, [placeId]);
        await handler.HandleAsync(processingMessage);

        dbContext.ChangeTracker.Clear();
        busMock.Invocations.Clear();

        // Now trigger TryComplete
        var message = new ImportProcessCompleted(import.Id, userId, archiveJobId);

        // Act
        await handler.HandleAsync(message);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        registerResult.IsSuccess.ShouldBeTrue();
        processResult.IsSuccess.ShouldBeTrue();
        var reloadedImport = await dbContext.Imports.SingleAsync(i => i.UserId == userId);
        reloadedImport.State.ShouldBe(ImportState.Complete);
        busMock.Invocations.Any(i => i.Arguments.Count > 0 && i.Arguments[0] is ImportCompleted).ShouldBeTrue();
    }

    private static async Task<(ImportProcessingHandler handler, PinsDbContext dbContext, Mock<IMessageContext> busMock,
        Mock<IImportConcurrencyPolicy> concurrencyPolicyMock, Mock<IStaredPlaceValidator> validatorMock)> CreateHandlerAsync()
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
        var validatorMock = new Mock<IStaredPlaceValidator>();

        // sensible defaults
        concurrencyPolicyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ImportProcessingHandler(logger, dbContext, busMock.Object, validatorMock.Object);

        return (handler, dbContext, busMock, concurrencyPolicyMock, validatorMock);
    }
}