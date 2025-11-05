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
        policyMock.Setup(p => p.CanStartImportAsync(userId, CancellationToken.None)).ReturnsAsync(true);

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
        policyMock.Setup(p => p.CanStartImportAsync(userId, CancellationToken.None)).ReturnsAsync(true);

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
        policyMock.Setup(p => p.CanStartImportAsync(userId, CancellationToken.None)).ReturnsAsync(true);

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
        policyMock.Setup(p => p.CanStartImportAsync(userId, CancellationToken.None)).ReturnsAsync(true);

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
        policyMock.Setup(p => p.CanStartImportAsync(userId, CancellationToken.None)).ReturnsAsync(false);

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
        policyMock.Setup(p => p.CanStartImportAsync(userId, CancellationToken.None)).ReturnsAsync(true);

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
    public void AppendBatch_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

        // Act
        var result = import.AppendBatch(processed: 10, created: 5, updated: 3, failed: 1, conflicts: 1);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        import.Processed.ShouldBe(10);
        import.Created.ShouldBe(5);
        import.Updated.ShouldBe(3);
        import.Failed.ShouldBe(1);
        import.Conflicts.ShouldBe(1);

        var evt = import.DomainEvents.Last().ShouldBeOfType<ImportBatchProcessed>();
        evt.AggregateId.ShouldBe(import.Id);
        evt.Processed.ShouldBe(10);
        evt.Created.ShouldBe(5);
        evt.Updated.ShouldBe(3);
        evt.Failed.ShouldBe(1);
        evt.Conflicts.ShouldBe(1);
    }

    [Test]
    public void AppendBatch_accumulates_counters_across_multiple_batches()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

        // Act
        import.AppendBatch(processed: 10, created: 5, updated: 3, failed: 1, conflicts: 1);
        import.AppendBatch(processed: 20, created: 10, updated: 7, failed: 2, conflicts: 1);

        // Assert
        import.Processed.ShouldBe(30);
        import.Created.ShouldBe(15);
        import.Updated.ShouldBe(10);
        import.Failed.ShouldBe(3);
        import.Conflicts.ShouldBe(2);
        import.DomainEvents.Count.ShouldBe(3); // StartAsync + 2 AppendBatch
    }

    [Test]
    public void AppendBatch_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var import = new Import("user123");

        // Act
        var result = import.AppendBatch(processed: 10, created: 5, updated: 3, failed: 1, conflicts: 1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not in progress"));
        import.Processed.ShouldBe(0);
        import.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public void AppendBatch_fails_when_processed_is_negative()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

        // Act
        var result = import.AppendBatch(processed: -1, created: 5, updated: 3, failed: 1, conflicts: 1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Batch counters must be non-negative"));
        import.Processed.ShouldBe(0);
    }

    [Test]
    public void AppendBatch_fails_when_created_is_negative()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

        // Act
        var result = import.AppendBatch(processed: 10, created: -1, updated: 3, failed: 1, conflicts: 1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Batch counters must be non-negative"));
        import.Created.ShouldBe(0);
    }

    [Test]
    public void AppendBatch_fails_when_updated_is_negative()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

        // Act
        var result = import.AppendBatch(processed: 10, created: 5, updated: -1, failed: 1, conflicts: 1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Batch counters must be non-negative"));
        import.Updated.ShouldBe(0);
    }

    [Test]
    public void AppendBatch_fails_when_failed_is_negative()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

        // Act
        var result = import.AppendBatch(processed: 10, created: 5, updated: 3, failed: -1, conflicts: 1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Batch counters must be non-negative"));
        import.Failed.ShouldBe(0);
    }

    [Test]
    public void AppendBatch_fails_when_conflicts_is_negative()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

        // Act
        var result = import.AppendBatch(processed: 10, created: 5, updated: 3, failed: 1, conflicts: -1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Batch counters must be non-negative"));
        import.Conflicts.ShouldBe(0);
    }

    [Test]
    public void Complete_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

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
    public void Complete_fails_when_state_is_not_in_progress()
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
    public void Fail_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var import = Imports.CreateStartedImport();
        var errorMessage = "Something went wrong";

        // Act
        var result = import.Fail(errorMessage);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        import.State.ShouldBe(ImportState.Failed);
        import.CompletedAt.ShouldNotBeNull();

        var evt = import.DomainEvents.Last().ShouldBeOfType<ImportFailed>();
        evt.AggregateId.ShouldBe(import.Id);
        evt.Error.ShouldBe(errorMessage);
    }

    [Test]
    public void Fail_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var import = new Import("user123");

        // Act
        var result = import.Fail("Error message");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not in progress"));
        import.State.ShouldBe(ImportState.Unspecified);
        import.CompletedAt.ShouldBeNull();
    }

    [Test]
    public void Fail_fails_when_error_message_is_null()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

        // Act
        var result = import.Fail(null!);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Error message cannot be empty"));
        import.State.ShouldBe(ImportState.InProgress);
        import.CompletedAt.ShouldBeNull();
    }

    [Test]
    public void Fail_fails_when_error_message_is_empty()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

        // Act
        var result = import.Fail("");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Error message cannot be empty"));
        import.State.ShouldBe(ImportState.InProgress);
        import.CompletedAt.ShouldBeNull();
    }

    [Test]
    public void Fail_fails_when_error_message_is_whitespace()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

        // Act
        var result = import.Fail("   ");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Error message cannot be empty"));
        import.State.ShouldBe(ImportState.InProgress);
        import.CompletedAt.ShouldBeNull();
    }

    [Test]
    public void Cancel_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

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
    public void UpdateTotal_updates_total_when_state_is_in_progress()
    {
        // Arrange
        var import = Imports.CreateStartedImport();

        // Act
        import.UpdateTotal(100);
        import.UpdateTotal(50);

        // Assert
        import.Total.ShouldBe(150u);
    }

    [Test]
    public void UpdateTotal_does_not_update_when_state_is_not_in_progress()
    {
        // Arrange
        var import = new Import("user123");

        // Act
        import.UpdateTotal(100);

        // Assert
        import.Total.ShouldBe(0u);
    }

    [Test]
    public void UpdateTotal_does_not_update_after_completion()
    {
        // Arrange
        var import = Imports.CreateStartedImport();
        import.UpdateTotal(100);
        import.AppendBatch(processed: 100, created: 0, updated: 0, failed: 0, conflicts: 0);
        var completeResult = import.TryComplete();
        completeResult.IsSuccess.ShouldBeTrue();
        completeResult.Value.ShouldBeTrue();

        // Act
        import.UpdateTotal(50);

        // Assert
        import.Total.ShouldBe(100u);
    }

    [Test]
    public void UpdateTotal_does_not_update_after_failure()
    {
        // Arrange
        var import = Imports.CreateStartedImport();
        import.UpdateTotal(100);
        import.Fail("Error");

        // Act
        import.UpdateTotal(50);

        // Assert
        import.Total.ShouldBe(100u);
    }

    [Test]
    public void UpdateTotal_does_not_update_after_cancellation()
    {
        // Arrange
        var import = Imports.CreateStartedImport();
        import.UpdateTotal(100);
        import.Cancel();

        // Act
        import.UpdateTotal(50);

        // Assert
        import.Total.ShouldBe(100u);
    }

    [Test]
    public void Complete_fails_when_not_all_items_processed()
    {
        // Arrange
        var import = Imports.CreateStartedImport();
        import.UpdateTotal(100);
        import.AppendBatch(processed: 50, created: 0, updated: 0, failed: 0, conflicts: 0);

        // Act
        var result = import.TryComplete();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
        import.State.ShouldBe(ImportState.InProgress);
        import.DomainEvents.OfType<ImportCompleted>().ShouldBeEmpty();
    }

    [Test]
    public void ReportConflictsAndFailures_adds_items_to_collections()
    {
        // Arrange
        var import = Imports.CreateStartedImport();
        var now = DateTimeOffset.UtcNow;
        var conflicts = new[]
        {
            new ReportedPlace("https://maps.google.com/1", now), new ReportedPlace("https://maps.google.com/2", now.AddMinutes(1))
        };
        var failures = new[] { new ReportedPlace("https://maps.google.com/3", now.AddMinutes(2)) };

        // Act
        import.ReportConflictsAndFailures(conflicts, failures);

        // Assert
        import.ConflictedPlaces.Select(p => p.MapsUrl)
            .ShouldBe(["https://maps.google.com/1", "https://maps.google.com/2"], ignoreOrder: true);
        import.FailedPlaces.Select(p => p.MapsUrl).ShouldBe(["https://maps.google.com/3"]);
    }


    [Test]
    public void Period_defaults_to_AllTime_when_not_provided()
    {
        // Arrange & Act
        var before = DateTimeOffset.UtcNow;
        var import = new Import("user123");
        var after = DateTimeOffset.UtcNow;

        // Assert
        import.Period.Start.ShouldBe(DateTimeOffset.MinValue);
        import.Period.End.ShouldBeGreaterThanOrEqualTo(before);
        import.Period.End.ShouldBeLessThanOrEqualTo(after);
    }

    [Test]
    public void Period_is_set_when_provided_in_constructor()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddDays(-7);
        var end = DateTimeOffset.UtcNow.AddDays(-1);
        var periodResult = Period.Create(start, end);
        periodResult.IsSuccess.ShouldBeTrue();
        var period = periodResult.Value;

        // Act
        var import = new Import("user123", period);

        // Assert
        import.Period.ShouldBe(period);
    }

    [Test]
    public async Task Period_is_not_modified_by_state_changes()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddDays(-30);
        var end = DateTimeOffset.UtcNow.AddDays(-10);
        var period = Period.Create(start, end).Value;
        var import = new Import("user123", period);

        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(import.UserId, CancellationToken.None)).ReturnsAsync(true);

        // Act
        var startResult = await import.StartAsync("archive456", policyMock.Object);
        startResult.IsSuccess.ShouldBeTrue();
        import.UpdateTotal(2);
        import.AppendBatch(2, 1, 1, 0, 0);
        var completeResult = import.TryComplete();

        // Assert
        completeResult.IsSuccess.ShouldBeTrue();
        completeResult.Value.ShouldBeTrue();
        import.Period.ShouldBe(period);
    }
}