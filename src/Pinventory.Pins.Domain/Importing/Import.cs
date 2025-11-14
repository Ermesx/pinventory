using System.ComponentModel.DataAnnotations.Schema;

using FluentResults;

using Pinventory.Pins.Domain.Abstractions;
using Pinventory.Pins.Domain.Importing.Events;

namespace Pinventory.Pins.Domain.Importing;

public sealed class Import(string userId, Period? period = null, Guid? id = null) : AggregateRoot(id)
{
    private readonly List<Batch> _batches = [];
    private Import() : this(string.Empty, Period.AllTime) { }

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
    public int Total { get; private set; }

    [NotMapped]
    public IReadOnlyCollection<StarredPlace> ConflictedPlaces =>
        _batches.SelectMany(x => x.StarredPlaces).Where(x => x.State == StarredPlaceState.Conflicted).ToList();

    [NotMapped]
    public IReadOnlyCollection<StarredPlace> FailedPlaces =>
        _batches.SelectMany(x => x.StarredPlaces).Where(x => x.State == StarredPlaceState.Invalid).ToList();

    [NotMapped]
    public IReadOnlyDictionary<Guid, IReadOnlyCollection<StarredPlace>> Batches =>
        _batches.ToDictionary(x => x.Id, x => x.StarredPlaces);

    public async Task<Result<Success>> StartAsync(string archiveJobId, IImportConcurrencyPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(archiveJobId))
        {
            return Result.Fail(Errors.Import.ArchiveJobIdCannotBeEmpty());
        }

        if (State != ImportState.Unspecified || !await policy.CanStartImportAsync(UserId))
        {
            return Result.Fail(Errors.Import.ImportAlreadyStartedOrFinished(this));
        }

        State = ImportState.InProgress;
        ArchiveJobId = archiveJobId;
        StartedAt = DateTimeOffset.UtcNow;

        Raise(new ImportStarted(Id, UserId, ArchiveJobId));

        return Result.Ok();
    }

    public Result<Success> RegisterBatch(IReadOnlyList<StarredPlace> starredPlaces)
    {
        if (State != ImportState.InProgress)
        {
            return Result.Fail(Errors.Import.CannotRegisterBatch(State));
        }

        if (!starredPlaces.Any())
        {
            return Result.Fail(Errors.Import.BatchCannotBeEmpty());
        }

        var batch = new Batch(starredPlaces);
        _batches.Add(batch);

        Total += batch.StarredPlaces.Count;

        Raise(new ImportBatchRegistered(Id, UserId, ArchiveJobId, batch.Id));

        return Result.Ok();
    }

    public async Task<Result<(IEnumerable<StarredPlace> ToCreate, IEnumerable<StarredPlace> ToUpdate)>> ProcessBatchAsync(Guid batchId,
        IStaredPlaceValidator validator, CancellationToken cancellationToken = default)
    {
        if (State != ImportState.InProgress)
        {
            return Result.Fail(Errors.Import.ImportNotInProgress(this));
        }

        if (!Batches.TryGetValue(batchId, out var batch))
        {
            return Result.Fail(Errors.Import.BatchNotExists(batchId, this));
        }

        int processed = 0, created = 0, updated = 0, failed = 0, conflicts = 0;
        var toCreate = new List<StarredPlace>();
        var toUpdate = new List<StarredPlace>();
        foreach (var place in batch.Where(x => !x.IsProcessed))
        {
            place.State = await validator.ValidateAsync(this, place, cancellationToken);
            place.IsProcessed = true;
            processed++;
            switch (place.State)
            {
                case StarredPlaceState.Invalid:
                    failed++;
                    continue;
                case StarredPlaceState.Conflicted:
                    conflicts++;
                    continue;
                case StarredPlaceState.New:
                    toCreate.Add(place);
                    created++;
                    continue;
                case StarredPlaceState.Exists:
                    toUpdate.Add(place);
                    updated++;
                    continue;
            }
        }

        Processed += processed;
        Created += created;
        Updated += updated;
        Failed += failed;
        Conflicts += conflicts;

        Raise(new ImportBatchProcessed(Id, UserId, ArchiveJobId!, processed, created, updated, failed, conflicts, Total));

        return (toCreate, toUpdate);
    }

    public Result<bool> TryComplete()
    {
        if (State != ImportState.InProgress)
        {
            return Result.Fail(Errors.Import.ImportNotInProgress(this));
        }

        if (_batches.SelectMany(x => x.StarredPlaces).Any(x => !x.IsProcessed))
        {
            return false;
        }

        State = ImportState.Complete;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new ImportCompleted(Id, UserId, ArchiveJobId!));

        return true;
    }

    public Result<Success> Fail(IError error)
    {
        if (State != ImportState.InProgress)
        {
            return Result.Fail(Errors.Import.ImportNotInProgress(this));
        }

        State = ImportState.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new ImportFailed(Id, UserId, ArchiveJobId!, error.Message));

        return Result.Ok();
    }

    public Result<Success> Cancel()
    {
        if (State != ImportState.InProgress)
        {
            return Result.Fail(Errors.Import.ImportNotInProgress(this));
        }

        State = ImportState.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new ImportCancelled(Id, UserId, ArchiveJobId!));

        return Result.Ok();
    }
}