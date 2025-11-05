using Moq;

using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Importing.Events;
using Pinventory.Pins.Domain.UnitTests.TestUtils;

using Shouldly;

namespace Pinventory.Pins.Domain.UnitTests;

public class ImportJobTests
{
    [Test]
    public async Task StartAsync_succeeds_and_raises_event_when_state_is_unspecified_and_policy_allows()
    {
        // Arrange
        var userId = "user123";
        var archiveJobId = "archive456";
        var importJob = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, default)).ReturnsAsync(true);

        // Act
        var result = await importJob.StartAsync(archiveJobId, policyMock.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        importJob.State.ShouldBe(ImportState.InProgress);
        importJob.ArchiveJobId.ShouldBe(archiveJobId);
        importJob.StartedAt.ShouldNotBeNull();
        importJob.CompletedAt.ShouldBeNull();

        var evt = importJob.DomainEvents.Last().ShouldBeOfType<ImportStarted>();
        evt.AggregateId.ShouldBe(importJob.Id);
        evt.UserId.ShouldBe(userId);
        evt.ArchiveJobId.ShouldBe(archiveJobId);
    }

    [Test]
    public async Task StartAsync_fails_when_archiveJobId_is_null()
    {
        // Arrange
        var userId = "user123";
        var importJob = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, default)).ReturnsAsync(true);

        // Act
        var result = await importJob.StartAsync(null!, policyMock.Object);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Archive job id cannot be empty"));
        importJob.State.ShouldBe(ImportState.Unspecified);
        importJob.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public async Task StartAsync_fails_when_archiveJobId_is_empty()
    {
        // Arrange
        var userId = "user123";
        var importJob = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, default)).ReturnsAsync(true);

        // Act
        var result = await importJob.StartAsync("", policyMock.Object);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Archive job id cannot be empty"));
        importJob.State.ShouldBe(ImportState.Unspecified);
        importJob.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public async Task StartAsync_fails_when_archiveJobId_is_whitespace()
    {
        // Arrange
        var userId = "user123";
        var importJob = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, default)).ReturnsAsync(true);

        // Act
        var result = await importJob.StartAsync("   ", policyMock.Object);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Archive job id cannot be empty"));
        importJob.State.ShouldBe(ImportState.Unspecified);
        importJob.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public async Task StartAsync_fails_when_policy_does_not_allow()
    {
        // Arrange
        var userId = "user123";
        var archiveJobId = "archive456";
        var importJob = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, default)).ReturnsAsync(false);

        // Act
        var result = await importJob.StartAsync(archiveJobId, policyMock.Object);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Import already started or finished"));
        importJob.State.ShouldBe(ImportState.Unspecified);
        importJob.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public async Task StartAsync_fails_when_state_is_already_in_progress()
    {
        // Arrange
        var userId = "user123";
        var archiveJobId = "archive456";
        var importJob = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(userId, default)).ReturnsAsync(true);

        await importJob.StartAsync(archiveJobId, policyMock.Object);

        // Act
        var result = await importJob.StartAsync("archive789", policyMock.Object);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Import already started or finished"));
        importJob.State.ShouldBe(ImportState.InProgress);
        importJob.ArchiveJobId.ShouldBe(archiveJobId);
        importJob.DomainEvents.Count.ShouldBe(1);
    }

    [Test]
    public void AppendBatch_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        var result = importJob.AppendBatch(processed: 10, created: 5, updated: 3, failed: 1, conflicts: 1);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        importJob.Processed.ShouldBe(10);
        importJob.Created.ShouldBe(5);
        importJob.Updated.ShouldBe(3);
        importJob.Failed.ShouldBe(1);
        importJob.Conflicts.ShouldBe(1);

        var evt = importJob.DomainEvents.Last().ShouldBeOfType<ImportBatchProcessed>();
        evt.AggregateId.ShouldBe(importJob.Id);
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
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        importJob.AppendBatch(processed: 10, created: 5, updated: 3, failed: 1, conflicts: 1);
        importJob.AppendBatch(processed: 20, created: 10, updated: 7, failed: 2, conflicts: 1);

        // Assert
        importJob.Processed.ShouldBe(30);
        importJob.Created.ShouldBe(15);
        importJob.Updated.ShouldBe(10);
        importJob.Failed.ShouldBe(3);
        importJob.Conflicts.ShouldBe(2);
        importJob.DomainEvents.Count.ShouldBe(3); // StartAsync + 2 AppendBatch
    }

    [Test]
    public void AppendBatch_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var importJob = new Import("user123");

        // Act
        var result = importJob.AppendBatch(processed: 10, created: 5, updated: 3, failed: 1, conflicts: 1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Import is not in progress"));
        importJob.Processed.ShouldBe(0);
        importJob.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public void AppendBatch_fails_when_processed_is_negative()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        var result = importJob.AppendBatch(processed: -1, created: 5, updated: 3, failed: 1, conflicts: 1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Batch counters must be non-negative"));
        importJob.Processed.ShouldBe(0);
    }

    [Test]
    public void AppendBatch_fails_when_created_is_negative()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        var result = importJob.AppendBatch(processed: 10, created: -1, updated: 3, failed: 1, conflicts: 1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Batch counters must be non-negative"));
        importJob.Created.ShouldBe(0);
    }

    [Test]
    public void AppendBatch_fails_when_updated_is_negative()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        var result = importJob.AppendBatch(processed: 10, created: 5, updated: -1, failed: 1, conflicts: 1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Batch counters must be non-negative"));
        importJob.Updated.ShouldBe(0);
    }

    [Test]
    public void AppendBatch_fails_when_failed_is_negative()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        var result = importJob.AppendBatch(processed: 10, created: 5, updated: 3, failed: -1, conflicts: 1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Batch counters must be non-negative"));
        importJob.Failed.ShouldBe(0);
    }

    [Test]
    public void AppendBatch_fails_when_conflicts_is_negative()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        var result = importJob.AppendBatch(processed: 10, created: 5, updated: 3, failed: 1, conflicts: -1);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Batch counters must be non-negative"));
        importJob.Conflicts.ShouldBe(0);
    }

    [Test]
    public void Complete_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        var result = importJob.TryComplete();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        importJob.State.ShouldBe(ImportState.Complete);
        importJob.CompletedAt.ShouldNotBeNull();

        var evt = importJob.DomainEvents.Last().ShouldBeOfType<ImportCompleted>();
        evt.AggregateId.ShouldBe(importJob.Id);
    }

    [Test]
    public void Complete_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var importJob = new Import("user123");

        // Act
        var result = importJob.TryComplete();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Import is not in progress"));
        importJob.State.ShouldBe(ImportState.Unspecified);
        importJob.CompletedAt.ShouldBeNull();
    }

    [Test]
    public void Fail_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();
        var errorMessage = "Something went wrong";

        // Act
        var result = importJob.Fail(errorMessage);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        importJob.State.ShouldBe(ImportState.Failed);
        importJob.CompletedAt.ShouldNotBeNull();

        var evt = importJob.DomainEvents.Last().ShouldBeOfType<ImportFailed>();
        evt.AggregateId.ShouldBe(importJob.Id);
        evt.Error.ShouldBe(errorMessage);
    }

    [Test]
    public void Fail_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var importJob = new Import("user123");

        // Act
        var result = importJob.Fail("Error message");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Import is not in progress"));
        importJob.State.ShouldBe(ImportState.Unspecified);
        importJob.CompletedAt.ShouldBeNull();
    }

    [Test]
    public void Fail_fails_when_error_message_is_null()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        var result = importJob.Fail(null!);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Error message cannot be empty"));
        importJob.State.ShouldBe(ImportState.InProgress);
        importJob.CompletedAt.ShouldBeNull();
    }

    [Test]
    public void Fail_fails_when_error_message_is_empty()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        var result = importJob.Fail("");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Error message cannot be empty"));
        importJob.State.ShouldBe(ImportState.InProgress);
        importJob.CompletedAt.ShouldBeNull();
    }

    [Test]
    public void Fail_fails_when_error_message_is_whitespace()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        var result = importJob.Fail("   ");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Error message cannot be empty"));
        importJob.State.ShouldBe(ImportState.InProgress);
        importJob.CompletedAt.ShouldBeNull();
    }

    [Test]
    public void Cancel_succeeds_and_raises_event_when_state_is_in_progress()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        var result = importJob.Cancel();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        importJob.State.ShouldBe(ImportState.Cancelled);
        importJob.CompletedAt.ShouldNotBeNull();

        var evt = importJob.DomainEvents.Last().ShouldBeOfType<ImportCancelled>();
        evt.AggregateId.ShouldBe(importJob.Id);
    }

    [Test]
    public void Cancel_fails_when_state_is_not_in_progress()
    {
        // Arrange
        var importJob = new Import("user123");

        // Act
        var result = importJob.Cancel();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Import is not in progress"));
        importJob.State.ShouldBe(ImportState.Unspecified);
        importJob.CompletedAt.ShouldBeNull();
    }

    [Test]
    public void UpdateTotal_updates_total_when_state_is_in_progress()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();

        // Act
        importJob.UpdateTotal(100);
        importJob.UpdateTotal(50);

        // Assert
        importJob.Total.ShouldBe(150u);
    }

    [Test]
    public void UpdateTotal_does_not_update_when_state_is_not_in_progress()
    {
        // Arrange
        var importJob = new Import("user123");

        // Act
        importJob.UpdateTotal(100);

        // Assert
        importJob.Total.ShouldBe(0u);
    }

    [Test]
    public void UpdateTotal_does_not_update_after_completion()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();
        importJob.UpdateTotal(100);
        importJob.TryComplete();

        // Act
        importJob.UpdateTotal(50);

        // Assert
        importJob.Total.ShouldBe(100u);
    }

    [Test]
    public void UpdateTotal_does_not_update_after_failure()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();
        importJob.UpdateTotal(100);
        importJob.Fail("Error");

        // Act
        importJob.UpdateTotal(50);

        // Assert
        importJob.Total.ShouldBe(100u);
    }

    [Test]
    public void UpdateTotal_does_not_update_after_cancellation()
    {
        // Arrange
        var importJob = ImportJobs.CreateStartedImportJob();
        importJob.UpdateTotal(100);
        importJob.Cancel();

        // Act
        importJob.UpdateTotal(50);

        // Assert
        importJob.Total.ShouldBe(100u);
    }
}