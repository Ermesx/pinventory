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
        evt.AggregateId.ShouldBe(import.Id);
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
    public async Task RegisterBatch_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow, null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow, null)
        };

        // Act
        var result = import.RegisterBatch(starredPlaces);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        import.Total.ShouldBe(2);
        import.Batches.Count.ShouldBe(1);

        var evt = import.DomainEvents.Last().ShouldBeOfType<ImportBatchRegistered>();
        evt.AggregateId.ShouldBe(import.Id);
    }

    [Test]
    public async Task RegisterBatch_accumulates_total_across_multiple_batches()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var batch1 = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow, null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow, null)
        };
        var batch2 = new List<StarredPlace>
        {
            new("Place 3", "https://maps.google.com/3", null, null, null, null, DateTimeOffset.UtcNow, null)
        };

        // Act
        import.RegisterBatch(batch1);
        import.RegisterBatch(batch2);

        // Assert
        import.Total.ShouldBe(3);
        import.Batches.Count.ShouldBe(2);
        import.DomainEvents.Count.ShouldBe(3); // StartAsync + 2 RegisterBatch
    }

    [Test]
    public void RegisterBatch_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var import = new Import("user123");
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow, null)
        };

        // Act
        var result = import.RegisterBatch(starredPlaces);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Cannot register batch"));
        import.Total.ShouldBe(0);
        import.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public async Task RegisterBatch_fails_when_batch_is_empty()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>();

        // Act
        var result = import.RegisterBatch(starredPlaces);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Batch cannot be empty"));
        import.Total.ShouldBe(0);
    }

    [Test]
    public async Task ProcessBatchAsync_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow, null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow, null)
        };
        var registerResult = import.RegisterBatch(starredPlaces);
        registerResult.IsSuccess.ShouldBeTrue();
        var batchId = import.Batches.Keys.First();

        var validatorMock = new Mock<IStaredPlaceValidator>();
        validatorMock.Setup(v => v.ValidateAsync(import, It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);

        // Act
        var result = await import.ProcessBatchAsync(batchId, validatorMock.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        import.Processed.ShouldBe(2);
        import.Created.ShouldBe(2);
        import.Updated.ShouldBe(0);
        import.Failed.ShouldBe(0);
        import.Conflicts.ShouldBe(0);

        var evt = import.DomainEvents.Last().ShouldBeOfType<ImportBatchProcessed>();
        evt.AggregateId.ShouldBe(import.Id);
        evt.Processed.ShouldBe(2);
        evt.Created.ShouldBe(2);
        evt.Updated.ShouldBe(0);
        evt.Failed.ShouldBe(0);
        evt.Conflicts.ShouldBe(0);
        evt.Total.ShouldBe(2);
    }

    [Test]
    public async Task ProcessBatchAsync_accumulates_counters_across_multiple_batches()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var batch1 = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow, null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow, null)
        };
        var batch2 = new List<StarredPlace>
        {
            new("Place 3", "https://maps.google.com/3", null, null, null, null, DateTimeOffset.UtcNow, null)
        };
        import.RegisterBatch(batch1);
        import.RegisterBatch(batch2);
        var batchIds = import.Batches.Keys.ToList();

        var validatorMock = new Mock<IStaredPlaceValidator>();
        validatorMock.Setup(v => v.ValidateAsync(import, It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);

        // Act
        await import.ProcessBatchAsync(batchIds[0], validatorMock.Object);
        await import.ProcessBatchAsync(batchIds[1], validatorMock.Object);

        // Assert
        import.Processed.ShouldBe(3);
        import.Created.ShouldBe(3);
        import.Updated.ShouldBe(0);
        import.Failed.ShouldBe(0);
        import.Conflicts.ShouldBe(0);
    }

    [Test]
    public async Task TryComplete_succeeds_when_all_items_processed()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow, null)
        };
        import.RegisterBatch(starredPlaces);
        var batchId = import.Batches.Keys.First();
        var validatorMock = new Mock<IStaredPlaceValidator>();
        validatorMock.Setup(v => v.ValidateAsync(import, It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);
        await import.ProcessBatchAsync(batchId, validatorMock.Object);

        // Act
        var result = import.TryComplete();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
        import.State.ShouldBe(ImportState.Complete);
        import.CompletedAt.ShouldNotBeNull();

        var evt = import.DomainEvents.Last().ShouldBeOfType<ImportCompleted>();
        evt.AggregateId.ShouldBe(import.Id);
    }

    [Test]
    public void TryComplete_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var import = new Import("user123");

        // Act
        var result = import.TryComplete();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not in progress"));
        import.State.ShouldBe(ImportState.Unspecified);
        import.CompletedAt.ShouldBeNull();
    }

    [Test]
    public async Task TryComplete_returns_false_when_not_all_items_processed()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow, null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow, null)
        };
        import.RegisterBatch(starredPlaces);
        // Don't process the batch

        // Act
        var result = import.TryComplete();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
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
        evt.AggregateId.ShouldBe(import.Id);
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
        evt.AggregateId.ShouldBe(import.Id);
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
    public async Task ConflictedPlaces_returns_places_with_conflicted_state()
    {
        // Arrange
        var import = await Imports.CreateStartedImport();
        var starredPlaces = new List<StarredPlace>
        {
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow, null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow, null),
            new("Place 3", "https://maps.google.com/3", null, null, null, null, DateTimeOffset.UtcNow, null)
        };
        import.RegisterBatch(starredPlaces);
        var batchId = import.Batches.Keys.First();

        var validatorMock = new Mock<IStaredPlaceValidator>();
        var callCount = 0;
        validatorMock.Setup(v => v.ValidateAsync(import, It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount <= 2 ? StarredPlaceState.Conflicted : StarredPlaceState.New;
            });

        await import.ProcessBatchAsync(batchId, validatorMock.Object);

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
            new("Place 1", "https://maps.google.com/1", null, null, null, null, DateTimeOffset.UtcNow, null),
            new("Place 2", "https://maps.google.com/2", null, null, null, null, DateTimeOffset.UtcNow, null)
        };
        import.RegisterBatch(starredPlaces);
        var batchId = import.Batches.Keys.First();

        var validatorMock = new Mock<IStaredPlaceValidator>();
        validatorMock.Setup(v => v.ValidateAsync(import, It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.Invalid);

        await import.ProcessBatchAsync(batchId, validatorMock.Object);

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
        var before = DateTimeOffset.UtcNow;
        var import = new Import("user123");
        var after = DateTimeOffset.UtcNow;

        // Assert
        import.Period.Start.ShouldBe(DateTimeOffset.UnixEpoch);
        import.Period.End.ShouldBeGreaterThanOrEqualTo(before);
        import.Period.End.ShouldBeLessThanOrEqualTo(after);
    }
}