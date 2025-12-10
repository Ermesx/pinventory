using Microsoft.Extensions.Logging;

using Moq;

using Nager.Country;

using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Tagging.Messages;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Infrastructure.Importing.Messages;
using Pinventory.Pins.Infrastructure.Importing.Sagas.Messages;
using Pinventory.Pins.Infrastructure.Importing.Services;

using Shouldly;

using Wolverine.Persistence;

namespace Pinventory.Pins.Application.UnitTests.Importing;

public class ImportProcessingHandlerTests
{
    [Test]
    public async Task ProcessPlaces_creates_updates_conflicts_and_publishes_tagging()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, pinsToUpdateProviderMock, policyMock, validatorMock) = CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        // existing pins: one for conflict by name, one to update by place id
        var address = new Address("Addr X", Alpha2Code.PL);
        var location = new Location(10, 20);
        var updatePin = new Pin(userId, "OldName", new GooglePlaceId("333"), address, location, DateTimeOffset.UtcNow);
        pinsToUpdateProviderMock.Setup(p => p.GetPinsAsync(userId, It.IsAny<IEnumerable<GooglePlaceId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([updatePin]);

        var places = new[]
        {
            new StarredPlace("SameName", "http://maps.google.com/?cid=222", "Addr 1", Alpha2Code.PL, 1, 2,
                DateTimeOffset.UtcNow.AddDays(-1),
                null), // conflict by name
            new StarredPlace("NewName", "http://maps.google.com/?cid=333", "Addr 2", Alpha2Code.PL, 3, 4, DateTimeOffset.UtcNow.AddDays(-1),
                null), // update by placeId
            new StarredPlace("Created", "http://maps.google.com/?cid=444", "Addr 3", Alpha2Code.PL, 5, 6, DateTimeOffset.UtcNow.AddDays(-1),
                null), // create new
            new StarredPlace("Removed", "http://maps.google.com/?cid=555", "Addr 4", Alpha2Code.PL, 7, 8, DateTimeOffset.UtcNow.AddDays(-1),
                null) // failed
        };

        var registerResult = import.RegisterPlaces(places);

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
        var outgoingMessages = await handler.HandleAsync(message, import);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        registerResult.IsSuccess.ShouldBeTrue();
        import.Processed.ShouldBe(4);
        import.Created.ShouldBe(1);
        import.Updated.ShouldBe(1);
        import.Failed.ShouldBe(1);
        import.Conflicts.ShouldBe(1);

        // Two pins should be tagged (created + updated)
        outgoingMessages.Count(x => x is AssignTagsToPinMessage).ShouldBe(2);
    }

    [Test]
    public async Task ProcessPlaces_raises_events_adds_created_pins_and_publishes_tagging_when_job_not_yet_finished()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, pinsToUpdateProviderMock, policyMock, validatorMock) = CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        // existing pins: one for conflict by name, one to update by place id
        var address = new Address("Addr X", Alpha2Code.PL);
        var location = new Location(10, 20);
        var updatePin = new Pin(userId, "OldName", new GooglePlaceId("333"), address, location, DateTimeOffset.UtcNow.AddDays(-1));
        pinsToUpdateProviderMock.Setup(p => p.GetPinsAsync(userId, It.IsAny<IEnumerable<GooglePlaceId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([updatePin]);

        var places = new[]
        {
            new StarredPlace("SameName", "http://maps.google.com/?cid=222", "Addr 1", Alpha2Code.PL, 1, 2,
                DateTimeOffset.UtcNow.AddDays(-1),
                null), // conflict by name
            new StarredPlace("NewName", "http://maps.google.com/?cid=333", "Addr 2", Alpha2Code.PL, 3, 4, DateTimeOffset.UtcNow.AddDays(-1),
                null), // update by placeId
            new StarredPlace("Created", "http://maps.google.com/?cid=444", "Addr 3", Alpha2Code.PL, 5, 6, DateTimeOffset.UtcNow.AddDays(-1),
                null) // create new
        };

        var registerResult = import.RegisterPlaces(places);
        // Add more places to ensure TryComplete will fail
        import.RegisterPlaces([
            new StarredPlace("Extra", "http://maps.google.com/?cid=999", "Addr", Alpha2Code.PL, 9, 10, DateTimeOffset.UtcNow.AddDays(-1),
                null)
        ]);

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
        var outgoingMessages = await handler.HandleAsync(message, import);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        registerResult.IsSuccess.ShouldBeTrue();
        import.State.ShouldBe(ImportState.InProgress);
        outgoingMessages.OfType<UnitOfWork<Pin>>().Single().Any(x => x.Entity.Name == "Created").ShouldBeTrue();
        outgoingMessages.Count(x => x is AssignTagsToPinMessage).ShouldBe(3);
    }

    [Test]
    public async Task ProcessPlaces_does_nothing_when_running_import_not_found()
    {
        // Arrange
        var (handler, _, _, _) = CreateHandlerAsync();
        var message = new PlacesProcessingBatchMessage(Guid.NewGuid(), "user-1", "job-404", [Guid.NewGuid()]);

        // Act
        var outgoingMessages = await handler.HandleAsync(message, null);

        // Assert
        outgoingMessages.ShouldBeEmpty();
    }

    [Test]
    public async Task TryComplete_completes_import_when_all_places_processed()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, _, policyMock, validatorMock) = CreateHandlerAsync();

        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        var places = new[]
        {
            new StarredPlace("Name", "http://maps.google.com/?cid=111", "Addr", Alpha2Code.PL, 1, 2, DateTimeOffset.UtcNow.AddDays(-1),
                null)
        };

        var registerResult = import.RegisterPlaces(places);

        var placeId = import.StarredPlaces.Single().Id;
        var placesToProcess = new HashSet<Guid> { placeId };
        var processResult = await import.ProcessPlacesAsync(placesToProcess, validatorMock.Object);

        // Set up a validator
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<Import>(), It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);

        // First process the place to set Processed count
        var processingMessage = new PlacesProcessingBatchMessage(import.Id, userId, archiveJobId, [placeId]);
        await handler.HandleAsync(processingMessage, import);

        // Now trigger TryComplete
        var message = new ImportProcessCompleted(import.Id, userId, archiveJobId);

        // Act
        handler.Handle(message, import);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        registerResult.IsSuccess.ShouldBeTrue();
        processResult.IsSuccess.ShouldBeTrue();
        import.State.ShouldBe(ImportState.Complete);
    }

    private static (ImportProcessingHandler handler, Mock<IPinsToUpdateProvider>, Mock<IImportConcurrencyPolicy> concurrencyPolicyMock,
        Mock<IStarredPlaceValidator> validatorMock) CreateHandlerAsync()
    {
        var logger = Mock.Of<ILogger<ImportProcessingHandler>>();
        var concurrencyPolicyMock = new Mock<IImportConcurrencyPolicy>();
        var validatorMock = new Mock<IStarredPlaceValidator>();
        var pinsToUpdateProviderMock = new Mock<IPinsToUpdateProvider>();

        // sensible defaults
        concurrencyPolicyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ImportProcessingHandler(logger, pinsToUpdateProviderMock.Object, validatorMock.Object);

        return (handler, pinsToUpdateProviderMock, concurrencyPolicyMock, validatorMock);
    }
}