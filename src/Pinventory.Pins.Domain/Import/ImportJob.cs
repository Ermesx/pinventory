using FluentResults;

using Pinventory.Pins.Domain.Abstractions;
using Pinventory.Pins.Domain.Import.Events;

namespace Pinventory.Pins.Domain.Import;

public sealed class ImportJob(string userId, Guid? id = null) : AggregateRoot(id)
{
    private ImportJob() : this(string.Empty) { }

    // TODO: Add value objects for UserId and ArchiveJobId
    public string UserId { get; } = userId;

    public string? ArchiveJobId { get; private set; }
    public ImportJobState State { get; private set; } = ImportJobState.Unspecified;

    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int Processed { get; private set; }
    public int Created { get; private set; }
    public int Updated { get; private set; }
    public int Failed { get; private set; }
    public int Conflicts { get; private set; }
    public uint Total { get; private set; }

    public async Task<Result<Success>> StartAsync(string archiveJobId, IImportConcurrencyPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(archiveJobId))
        {
            return Result.Fail(Errors.ImportJob.ArchiveJobIdCannotBeEmpty());
        }

        if (State != ImportJobState.Unspecified || !await policy.CanStartImportAsync(UserId))
        {
            return Result.Fail(Errors.ImportJob.ImportAlreadyStartedOrFinished(State));
        }

        State = ImportJobState.InProgress;
        ArchiveJobId = archiveJobId;
        StartedAt = DateTimeOffset.UtcNow;

        Raise(new ImportStarted(Id, UserId, ArchiveJobId));

        return Result.Ok();
    }

    public Result<Success> AppendBatch(int processed, int created, int updated, int failed, int conflicts)
    {
        if (State != ImportJobState.InProgress)
        {
            return Result.Fail(Errors.ImportJob.ImportNotInProgress(State));
        }

        if (processed < 0 || created < 0 || updated < 0 || failed < 0 || conflicts < 0)
        {
            return Result.Fail(Errors.ImportJob.BatchCountersMustBeNonNegative());
        }

        Processed += processed;
        Created += created;
        Updated += updated;
        Failed += failed;
        Conflicts += conflicts;
        Raise(new ImportBatchProcessed(Id, processed, created, updated, failed, conflicts));

        return Result.Ok();
    }

    public Result<Success> Complete()
    {
        if (State != ImportJobState.InProgress)
        {
            return Result.Fail(Errors.ImportJob.ImportNotInProgress(State));
        }

        State = ImportJobState.Complete;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new ImportCompleted(Id));

        return Result.Ok();
    }

    public Result<Success> Fail(string error)
    {
        if (State != ImportJobState.InProgress)
        {
            return Result.Fail(Errors.ImportJob.ImportNotInProgress(State));
        }

        if (string.IsNullOrWhiteSpace(error))
        {
            return Result.Fail(Errors.ImportJob.ErrorMessageCannotBeEmpty());
        }

        State = ImportJobState.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new ImportFailed(Id, error));

        return Result.Ok();
    }

    public Result<Success> Cancel()
    {
        if (State != ImportJobState.InProgress)
        {
            return Result.Fail(Errors.ImportJob.ImportNotInProgress(State));
        }

        State = ImportJobState.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new ImportCancelled(Id));

        return Result.Ok();
    }

    public void UpdateTotal(uint count)
    {
        if (State != ImportJobState.InProgress)
        {
            return;
        }

        Total += count;
    }
}