using FluentResults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

using Pinventory.Pins.Application.Importing;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Importing.Messages;
using Pinventory.Pins.Application.Importing.Services;
using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Infrastructure;

using Shouldly;

using Wolverine;

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

        var (handler, dbContext, busMock, _, _, _) = await CreateHandlerAsync();

        var command = new StartImportCommand(userId, period.Start, period.End);

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
        var (handler, dbContext, busMock, _, _, policyMock) = await CreateHandlerAsync();

        policyMock.Setup(p => p.CanStartImportAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var period = Period.AllTime;
        var command = new StartImportCommand(userId, period.Start, period.End);

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
        var (handler, dbContext, busMock, _, serviceMock, policyMock) = await CreateHandlerAsync();

        // The cancel request must succeed externally for the import to be cancelled
        serviceMock.Setup(s => s.CancelJobAsync(archiveJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<Success>(null!));

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
        var (handler, _, busMock, _, _, _) = await CreateHandlerAsync();

        var command = new CancelImportCommand("user-1", "job-404");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not found"));
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task StartImport_fails_when_service_factory_fails()
    {
        // Arrange
        var userId = "user-1";
        var (handler, dbContext, busMock, factoryMock, _, _) = await CreateHandlerAsync();

        factoryMock.Setup(f => f.CreateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<IImportService>("factory failed"));

        var period = Period.AllTime;
        var command = new StartImportCommand(userId, period.Start, period.End);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        (await dbContext.Imports.CountAsync()).ShouldBe(0);
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task RenewImport_clears_places_and_reschedules_check_job_when_running_import_exists()
    {
        // Arrange
        var userId = "user-1";
        var archiveJobId = "job-123";
        var (handler, dbContext, busMock, _, _, policyMock) = await CreateHandlerAsync();

        // Seed running import with places
        var import = new Import(userId, Period.AllTime);
        var startResult = await import.StartAsync(archiveJobId, policyMock.Object);
        startResult.IsSuccess.ShouldBeTrue();

        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow, null)
        };
        import.RegisterPlaces(starredPlaces);
        import.StarredPlaces.Count.ShouldBe(1);

        await dbContext.Imports.AddAsync(import);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var command = new RenewImportCommand(userId, archiveJobId);

        // Act
        var result = await handler.HandleAsync(command, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var reloaded = await dbContext.Imports.Include(i => i.StarredPlaces)
            .SingleAsync(i => i.UserId == userId);
        reloaded.StarredPlaces.Count.ShouldBe(0);
        reloaded.Total.ShouldBe(0);

        busMock.Invocations.Any(i => i.Arguments[0] is ImportPlacesCleared).ShouldBeTrue();
        busMock.Invocations.Any(i => i.Arguments[0] is CheckJobMessage).ShouldBeTrue();
    }

    [Test]
    public async Task RenewImport_fails_when_running_import_not_found()
    {
        // Arrange
        var (handler, _, busMock, _, _, _) = await CreateHandlerAsync();
        var command = new RenewImportCommand("user-1", "job-404");

        // Act
        var result = await handler.HandleAsync(command, null);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not found"));
        busMock.Invocations.Count.ShouldBe(0);
    }


    private static async Task<(ImportCommandHandler handler, PinsDbContext dbContext, Mock<IMessageContext> busMock,
        Mock<IImportServiceFactory>
        factoryMock, Mock<IImportService> serviceMock, Mock<IImportConcurrencyPolicy> concurrencyPolicyMock)> CreateHandlerAsync()
    {
        var options = new DbContextOptionsBuilder<PinsDbContext>()
            .UseSqlite(connectionString: "Data Source=:memory:")
            .Options;

        var dbContext = new PinsDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var logger = Mock.Of<ILogger<ImportCommandHandler>>();
        var busMock = new Mock<IMessageContext>();
        var factoryMock = new Mock<IImportServiceFactory>();
        var serviceMock = new Mock<IImportService>();
        var concurrencyPolicyMock = new Mock<IImportConcurrencyPolicy>();

        // sensible defaults
        serviceMock.Setup(s => s.InitiateAsync(It.IsAny<Period?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok("job-123"));
        factoryMock.Setup(f => f.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(serviceMock.Object));
        concurrencyPolicyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ImportCommandHandler(logger, factoryMock.Object, dbContext, busMock.Object, concurrencyPolicyMock.Object);

        return (handler, dbContext, busMock, factoryMock, serviceMock, concurrencyPolicyMock);
    }
}