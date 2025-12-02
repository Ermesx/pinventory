using FluentResults;

using Moq;

using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Domain.UnitTests.TestUtils;

using Shouldly;

namespace Pinventory.Pins.Domain.UnitTests;

public class ImportTests
{
    [Test]
    public async Task StartAsync_succeeds_and_raises_event_when_state_is_unspecified_and_policy_allows()
    {
        // Arrange
        var userId = "user123";
        var archiveJobId = "archive456";
        var import = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await import.StartAsync(archiveJobId, policyMock.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        import.State.ShouldBe(ImportState.InProgress);
        import.ArchiveJobId.ShouldBe(archiveJobId);
        import.StartedAt.ShouldNotBeNull();
        import.CompletedAt.ShouldBeNull();

        var evt = import.DomainEvents.Last().ShouldBeOfType<ImportStarted>();
        evt.Id.ShouldBe(import.Id);
        evt.UserId.ShouldBe(userId);
        evt.ArchiveJobId.ShouldBe(archiveJobId);
    }

    [Test]
    public async Task StartAsync_fails_when_archiveJobId_is_null()
    {
        // Arrange
        var userId = "user123";
        var import = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await import.StartAsync(null!, policyMock.Object);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Archive job id cannot be empty"));
        import.State.ShouldBe(ImportState.Unspecified);
        import.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public async Task StartAsync_fails_when_archiveJobId_is_empty()
    {
        // Arrange
        var userId = "user123";
        var import = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await import.StartAsync("", policyMock.Object);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Archive job id cannot be empty"));
        import.State.ShouldBe(ImportState.Unspecified);
        import.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public async Task StartAsync_fails_when_archiveJobId_is_whitespace()
    {
        // Arrange
        var userId = "user123";
        var import = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await import.StartAsync("   ", policyMock.Object);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Archive job id cannot be empty"));
        import.State.ShouldBe(ImportState.Unspecified);
        import.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public async Task StartAsync_fails_when_policy_does_not_allow()
    {
        // Arrange
        var userId = "user123";
        var archiveJobId = "archive456";
        var import = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await import.StartAsync(archiveJobId, policyMock.Object);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("already started or finished"));
        import.State.ShouldBe(ImportState.Unspecified);
        import.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public async Task StartAsync_fails_when_state_is_already_in_progress()
    {
        // Arrange
        var userId = "user123";
        var archiveJobId = "archive456";
        var import = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await import.StartAsync(archiveJobId, policyMock.Object);

        // Act
        var result = await import.StartAsync("archive789", policyMock.Object);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("already started or finished"));
        import.State.ShouldBe(ImportState.InProgress);
        import.ArchiveJobId.ShouldBe(archiveJobId);
        import.DomainEvents.Count.ShouldBe(1);
    }

    [Test]
    public async Task RegisterPlaces_succeeds_and_raises_events_when_state_is_in_progress()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };

        // Act
        var result = import.RegisterPlaces(starredPlaces);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        import.Total.ShouldBe(2);
        import.StarredPlaces.Count.ShouldBe(2);
        import.DomainEvents.OfType<ImportPlaceRegistered>().Count().ShouldBe(2);
    }

    [Test]
    public async Task RegisterPlaces_accumulates_total_across_multiple_calls_and_ignores_duplicates()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var place1 = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        var place2 = new List<StarredPlace>
        {
            new("Place 3", "https://maps.google.com/3", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };

        // Act
        import.RegisterPlaces(place1);
        import.RegisterPlaces(place2);

        // Assert
        import.Total.ShouldBe(3);
        import.StarredPlaces.Count.ShouldBe(3);
        import.DomainEvents.OfType<ImportPlaceRegistered>().Count().ShouldBe(3);
    }

    [Test]
    public void RegisterPlaces_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var import = new Import("user123");
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };

        // Act
        var result = import.RegisterPlaces(starredPlaces);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Cannot register places"));
        import.Total.ShouldBe(0);
        import.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public async Task RegisterPlaces_fails_when_places_is_empty()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>();

        // Act
        var result = import.RegisterPlaces(starredPlaces);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Places cannot be empty"));
        import.Total.ShouldBe(0);
    }

    [Test]
    public async Task RegisterPlaces_succeeds_silently_when_same_places_already_registered()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var date1 = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var date2 = new DateTimeOffset(2024, 2, 20, 14, 45, 0, TimeSpan.Zero);

        var firstPlace = new List<StarredPlace>
        {
            new("Cafe Central", "https://maps.google.com/?cid=111", null, null, null, null, date1, null),
            new("Restaurant", "https://maps.google.com/?cid=222", null, null, null, null, date2, null)
        };

        var duplicatePlace = new List<StarredPlace>
        {
            new("Restaurant", "https://maps.google.com/?cid=222", null, null, null, null, date2, null),
            new("Cafe Central", "https://maps.google.com/?cid=111", null, null, null, null, date1, null)
        };

        var firstResult = import.RegisterPlaces(firstPlace);
        firstResult.IsSuccess.ShouldBeTrue();
        import.DomainEvents.OfType<ImportPlaceRegistered>().Count().ShouldBe(2);

        // Act
        var duplicateResult = import.RegisterPlaces(duplicatePlace);

        // Assert
        duplicateResult.IsSuccess.ShouldBeTrue();
        import.Total.ShouldBe(2);
        import.StarredPlaces.Count.ShouldBe(2);
        import.DomainEvents.OfType<ImportPlaceRegistered>().Count().ShouldBe(2);
    }

    [Test]
    public async Task ProcessPlacesAsync_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        var registerResult = import.RegisterPlaces(starredPlaces);
        registerResult.IsSuccess.ShouldBeTrue();

        var validatorMock = new Mock<IStarredPlaceValidator>();
        validatorMock.Setup(v => v.ValidateAsync(import, It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);

        var placeIds = import.StarredPlaces.Select(x => x.Id).ToHashSet();

        // Act
        var result = await import.ProcessPlacesAsync(placeIds, validatorMock.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        import.Processed.ShouldBe(2);
        import.Created.ShouldBe(2);
        import.Updated.ShouldBe(0);
        import.Failed.ShouldBe(0);
        import.Conflicts.ShouldBe(0);

        import.DomainEvents.OfType<ImportPlaceProcessed>().Count().ShouldBe(2);
    }

    [Test]
    public async Task ProcessPlacesAsync_accumulates_counters_across_multiple_calls()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var place1 = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        var place2 = new List<StarredPlace>
        {
            new("Place 3", "https://maps.google.com/3", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        import.RegisterPlaces(place1);
        import.RegisterPlaces(place2);

        var validatorMock = new Mock<IStarredPlaceValidator>();
        validatorMock.Setup(v => v.ValidateAsync(import, It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);

        var firstPlaceIds = import.StarredPlaces.Take(2).Select(x => x.Id).ToHashSet();
        var secondPlaceIds = import.StarredPlaces.Skip(2).Select(x => x.Id).ToHashSet();

        // Act
        await import.ProcessPlacesAsync(firstPlaceIds, validatorMock.Object);
        await import.ProcessPlacesAsync(secondPlaceIds, validatorMock.Object);

        // Assert
        import.Processed.ShouldBe(3);
        import.Created.ShouldBe(3);
        import.Updated.ShouldBe(0);
        import.Failed.ShouldBe(0);
        import.Conflicts.ShouldBe(0);
    }

    [Test]
    public async Task Complete_succeeds_when_all_items_processed()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        import.RegisterPlaces(starredPlaces);
        var validatorMock = new Mock<IStarredPlaceValidator>();
        validatorMock.Setup(v => v.ValidateAsync(import, It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);

        var placeIds = import.StarredPlaces.Select(x => x.Id).ToHashSet();
        await import.ProcessPlacesAsync(placeIds, validatorMock.Object);

        // Act
        var result = import.Complete();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        import.State.ShouldBe(ImportState.Complete);
        import.CompletedAt.ShouldNotBeNull();

        var evt = import.DomainEvents.Last().ShouldBeOfType<ImportCompleted>();
        evt.Id.ShouldBe(import.Id);
    }

    [Test]
    public void Complete_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var import = new Import("user123");

        // Act
        var result = import.Complete();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not in progress"));
        import.State.ShouldBe(ImportState.Unspecified);
        import.CompletedAt.ShouldBeNull();
    }

    [Test]
    public async Task Complete_fails_when_not_all_items_processed()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        import.RegisterPlaces(starredPlaces);
        var validatorMock = new Mock<IStarredPlaceValidator>();
        validatorMock.Setup(v => v.ValidateAsync(import, It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);

        // Process only one place
        var firstPlaceId = import.StarredPlaces.First().Id;
        await import.ProcessPlacesAsync(new HashSet<Guid> { firstPlaceId }, validatorMock.Object);

        // Act
        var result = import.Complete();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Not all places processed"));
        import.State.ShouldBe(ImportState.InProgress);
        import.DomainEvents.OfType<ImportCompleted>().ShouldBeEmpty();
    }

    [Test]
    public async Task Fail_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var error = new Error("Something went wrong");

        // Act
        var result = import.Fail(error);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        import.State.ShouldBe(ImportState.Failed);
        import.CompletedAt.ShouldNotBeNull();

        var evt = import.DomainEvents.Last().ShouldBeOfType<ImportFailed>();
        evt.Id.ShouldBe(import.Id);
        evt.Error.ShouldBe(error.Message);
    }

    [Test]
    public void Fail_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var import = new Import("user123");

        // Act
        var result = import.Fail(new Error("Error message"));

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not in progress"));
        import.State.ShouldBe(ImportState.Unspecified);
        import.CompletedAt.ShouldBeNull();
    }

    [Test]
    public async Task Cancel_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();

        // Act
        var result = import.Cancel();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        import.State.ShouldBe(ImportState.Cancelled);
        import.CompletedAt.ShouldNotBeNull();

        var evt = import.DomainEvents.Last().ShouldBeOfType<ImportCancelled>();
        evt.Id.ShouldBe(import.Id);
    }

    [Test]
    public void Cancel_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var import = new Import("user123");

        // Act
        var result = import.Cancel();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not in progress"));
        import.State.ShouldBe(ImportState.Unspecified);
        import.CompletedAt.ShouldBeNull();
    }

    [Test]
    public async Task ClearPlaces_clears_places_and_raises_event_when_in_progress()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        import.RegisterPlaces(starredPlaces);
        import.StarredPlaces.Count.ShouldBe(2);
        import.Total.ShouldBe(2);

        // Act
        var result = import.ClearPlaces();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        import.StarredPlaces.Count.ShouldBe(0);
        import.Total.ShouldBe(0);

        var evt = import.DomainEvents.Last().ShouldBeOfType<ImportPlacesCleared>();
        evt.Id.ShouldBe(import.Id);
        evt.UserId.ShouldBe(import.UserId);
        evt.ArchiveJobId.ShouldBe(import.ArchiveJobId);
    }

    [Test]
    public void ClearPlaces_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var import = new Import("user123");

        // Act
        var result = import.ClearPlaces();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not in progress"));
        import.StarredPlaces.Count.ShouldBe(0);
        import.Total.ShouldBe(0);
        import.DomainEvents.OfType<ImportPlacesCleared>().ShouldBeEmpty();
    }


    [Test]
    public async Task ConflictedPlaces_returns_places_with_conflicted_state()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null),
            new("Place 3", "https://maps.google.com/3", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        import.RegisterPlaces(starredPlaces);

        var validatorMock = new Mock<IStarredPlaceValidator>();
        var callCount = 0;
        validatorMock.Setup(v => v.ValidateAsync(import, It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount <= 2 ? StarredPlaceState.Conflicted : StarredPlaceState.New;
            });

        var placeIds = import.StarredPlaces.Select(x => x.Id).ToHashSet();
        await import.ProcessPlacesAsync(placeIds, validatorMock.Object);

        // Act & Assert
        import.ConflictedPlaces.Count.ShouldBe(2);
        import.ConflictedPlaces.Select(p => p.GoogleMapsUrl)
            .ShouldBe(["https://maps.google.com/1", "https://maps.google.com/2"], ignoreOrder: true);
    }

    [Test]
    public async Task FailedPlaces_returns_places_with_invalid_state()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        import.RegisterPlaces(starredPlaces);

        var validatorMock = new Mock<IStarredPlaceValidator>();
        validatorMock.Setup(v => v.ValidateAsync(import, It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.Invalid);

        var placeIds = import.StarredPlaces.Select(x => x.Id).ToHashSet();
        await import.ProcessPlacesAsync(placeIds, validatorMock.Object);

        // Act & Assert
        // Note: FailedPlaces currently filters by Invalid state
        import.FailedPlaces.Count.ShouldBe(2);
        import.FailedPlaces.Select(p => p.GoogleMapsUrl)
            .ShouldBe(["https://maps.google.com/1", "https://maps.google.com/2"], ignoreOrder: true);
    }

    [Test]
    public void Period_defaults_to_AllTime_when_not_provided()
    {
        // Arrange & Act
        var before = DateTimeOffset.UtcNow.AddDays(-2);
        var import = new Import("user123");
        var after = DateTimeOffset.UtcNow;

        // Assert
        import.Period.Start.ShouldBe(DateTimeOffset.UnixEpoch);
        import.Period.End.ShouldBeGreaterThanOrEqualTo(before);
        import.Period.End.ShouldBeLessThanOrEqualTo(after);
    }
}