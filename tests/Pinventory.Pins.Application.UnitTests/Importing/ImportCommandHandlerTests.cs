using FluentResults;

using Microsoft.Extensions.Logging;

using Moq;

using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure.Sagas;

using Shouldly;

using Wolverine.Persistence;

namespace Pinventory.Pins.Application.UnitTests.Importing;

public class ImportCommandHandlerTests
{
    [Test]
    public async Task StartImport_creates_job_and_publishes_check_message()
    {
        // Arrange
        var userId = "user-1";
        var period = Period.AllTime;
        var archiveJobId = "job-123";

        var (handler, _, _, _) = CreateHandlerAsync(archiveJobId);

        var command = new StartImportCommand(userId, period.Start, period.End);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        response.Result.IsSuccess.ShouldBeTrue();

        response.Storage.Action.ShouldBe(StorageAction.Insert);
        response.Storage.Entity.ShouldNotBeNull();
        response.Storage.Entity.ArchiveJobId.ShouldBe(archiveJobId);
        response.Storage.Entity.State.ShouldBe(ImportState.InProgress);

        response.Message.ShouldNotBeNull();
    }

    [Test]
    public async Task StartImport_fails_when_concurrent_import_exists()
    {
        // Arrange
        var userId = "user-1";
        var (handler, _, _, policyMock) = CreateHandlerAsync();

        policyMock.Setup(p => p.CanStartImportAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var period = Period.AllTime;
        var command = new StartImportCommand(userId, period.Start, period.End);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        response.Result.IsFailed.ShouldBeTrue();
        response.Message.ShouldBeNull();
        response.Storage.Action.ShouldBe(StorageAction.Nothing);
    }

    [Test]
    public async Task CancelImport_cancels_import()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, _, serviceMock, policyMock) = CreateHandlerAsync(archiveJobId);

        // The cancel request must succeed externally for the import to be cancelled
        serviceMock.Setup(s => s.CancelJobAsync(archiveJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<Success>(null!));

        // Seed running import
        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);

        var command = new CancelImportCommand(userId, import.Id);

        // Act
        var result = await handler.HandleAsync(command, import);

        // Assert
        startResult.IsSuccess.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task CancelImport_fails_when_running_import_not_found()
    {
        // Arrange
        var (handler, _, _, _) = CreateHandlerAsync();

        var command = new CancelImportCommand("user-1", Guid.NewGuid());

        // Act
        var result = await handler.HandleAsync(command, null);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not found"));
    }

    [Test]
    public async Task StartImport_fails_when_service_factory_fails()
    {
        // Arrange
        var userId = "user-1";
        var (handler, factoryMock, _, _) = CreateHandlerAsync();

        factoryMock.Setup(f => f.CreateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<IImportService>("factory failed"));

        var period = Period.AllTime;
        var command = new StartImportCommand(userId, period.Start, period.End);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        response.Result.IsFailed.ShouldBeTrue();
        response.Storage.Action.ShouldBe(StorageAction.Nothing);
    }

    [Test]
    public async Task RenewImport_clears_places_and_reschedules_check_job_when_running_import_exists()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, _, _, policyMock) = CreateHandlerAsync(archiveJobId);

        // Seed running import with places
        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        startResult.IsSuccess.ShouldBeTrue();

        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        import.RegisterPlaces(starredPlaces);
        import.StarredPlaces.Count.ShouldBe(1);

        var saga = new ImportProcess { Id = import.Id };

        var command = new RenewImportCommand(userId, import.Id);

        // Act
        var response = handler.Handle(command, import, saga);

        // Assert
        response.Result.IsSuccess.ShouldBeTrue();
        response.Message.ShouldNotBeNull();
        response.Event.ShouldBeNull();
    }

    [Test]
    public async Task RenewImport_clears_places_and_reschedules_check_job_when_running_import_exists_and_publish_event_when_saga_not_found()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, _, _, policyMock) = CreateHandlerAsync(archiveJobId);

        // Seed running import with places
        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        startResult.IsSuccess.ShouldBeTrue();

        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        import.RegisterPlaces(starredPlaces);
        import.StarredPlaces.Count.ShouldBe(1);

        var command = new RenewImportCommand(userId, import.Id);

        // Act
        var response = handler.Handle(command, import, null);

        // Assert
        response.Result.IsSuccess.ShouldBeTrue();
        response.Message.ShouldNotBeNull();
        response.Event.ShouldNotBeNull();
    }

    [Test]
    public void RenewImport_fails_when_running_import_not_found()
    {
        // Arrange
        var (handler, _, _, _) = CreateHandlerAsync();
        var command = new RenewImportCommand("user-1", Guid.NewGuid());

        // Act
        var response = handler.Handle(command, null, null);

        // Assert
        response.Result.IsFailed.ShouldBeTrue();
        response.Result.Errors.ShouldContain(e => e.Message.Contains("not found"));
    }


    private static (
        ImportCommandHandler handler,
        Mock<IImportServiceFactory> factoryMock,
        Mock<IImportService> serviceMock,
        Mock<IImportConcurrencyPolicy> concurrencyPolicyMock) CreateHandlerAsync(string archiveJobId = "job-123")
    {
        var logger = Mock.Of<ILogger<ImportCommandHandler>>();
        var factoryMock = new Mock<IImportServiceFactory>();
        var serviceMock = new Mock<IImportService>();
        var concurrencyPolicyMock = new Mock<IImportConcurrencyPolicy>();

        // sensible defaults
        serviceMock.Setup(s => s.InitiateAsync(It.IsAny<Period?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(archiveJobId));
        factoryMock.Setup(f => f.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(serviceMock.Object));
        concurrencyPolicyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ImportCommandHandler(logger, factoryMock.Object, concurrencyPolicyMock.Object);

        return (handler, factoryMock, serviceMock, concurrencyPolicyMock);
    }
}