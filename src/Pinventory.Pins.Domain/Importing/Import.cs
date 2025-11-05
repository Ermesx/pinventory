using FluentResults;

using Pinventory.Pins.Domain.Abstractions;
using Pinventory.Pins.Domain.Importing.Events;

namespace Pinventory.Pins.Domain.Importing;

public sealed class Import(string userId, Period? period = null, Guid? id = null) : AggregateRoot(id)
{
    private readonly List<ReportedPlace> _conflictedPlaces = [];
    private readonly List<ReportedPlace> _failedPlaces = [];
    private Import() : this(string.Empty) { }

    // TODO: Add value objects for UserId and ArchiveJobId
    public string UserId { get; } = userId;
    public Period Period { get; private set; } = period ?? Period.AllTime;
    public string? ArchiveJobId { get; private set; }
    public ImportState State { get; private set; } = ImportState.Unspecified;
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int Processed { get; private set; }
    public int Created { get; private set; }
    public int Updated { get; private set; }
    public int Failed { get; private set; }
    public int Conflicts { get; private set; }
    public uint Total { get; private set; }
    public IReadOnlyCollection<ReportedPlace> ConflictedPlaces => _conflictedPlaces;
    public IReadOnlyCollection<ReportedPlace> FailedPlaces => _failedPlaces;

    public async Task<Result<Success>> StartAsync(string archiveJobId, IImportConcurrencyPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(archiveJobId))
        {
            return Result.Fail(Errors.ImportJob.ArchiveJobIdCannotBeEmpty());
        }

        if (State != ImportState.Unspecified || !await policy.CanStartImportAsync(UserId))
        {
            return Result.Fail(Errors.ImportJob.ImportAlreadyStartedOrFinished(this));
        }

        State = ImportState.InProgress;
        ArchiveJobId = archiveJobId;
        StartedAt = DateTimeOffset.UtcNow;

        Raise(new ImportStarted(Id, UserId, ArchiveJobId));

        return Result.Ok();
    }

    public Result<Success> AppendBatch(int processed, int created, int updated, int failed, int conflicts)
    {
        if (State != ImportState.InProgress)
        {
            return Result.Fail(Errors.ImportJob.ImportNotInProgress(this));
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

    public Result<Success> TryComplete()
    {
        if (Processed < Total)
        {
            return Result.Fail(Errors.ImportJob.ImportNotCompleteYet(this));
        }

        if (State != ImportState.InProgress)
        {
            return Result.Fail(Errors.ImportJob.ImportNotInProgress(this));
        }

        State = ImportState.Complete;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new ImportCompleted(Id));

        return Result.Ok();
    }

    public Result<Success> Fail(string error)
    {
        if (State != ImportState.InProgress)
        {
            return Result.Fail(Errors.ImportJob.ImportNotInProgress(this));
        }

        if (string.IsNullOrWhiteSpace(error))
        {
            return Result.Fail(Errors.ImportJob.ErrorMessageCannotBeEmpty());
        }

        State = ImportState.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new ImportFailed(Id, error));

        return Result.Ok();
    }

    public Result<Success> Cancel()
    {
        if (State != ImportState.InProgress)
        {
            return Result.Fail(Errors.ImportJob.ImportNotInProgress(this));
        }

        State = ImportState.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new ImportCancelled(Id));

        return Result.Ok();
    }

    public void UpdateTotal(uint count)
    {
        if (State != ImportState.InProgress)
        {
            return;
        }

        Total += count;
    }

    public void ReportConflictsAndFailures(IEnumerable<ReportedPlace> conflictingPlaces, IEnumerable<ReportedPlace> failedPlaces)
    {
        _conflictedPlaces.AddRange(conflictingPlaces);
        _failedPlaces.AddRange(failedPlaces);
    }
}