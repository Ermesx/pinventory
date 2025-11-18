using System.ComponentModel.DataAnnotations.Schema;

using FluentResults;

using Pinventory.Pins.Domain.Abstractions;
using Pinventory.Pins.Domain.Importing.Events;

namespace Pinventory.Pins.Domain.Importing;

public sealed class Import(string userId, Period? period = null, Guid? id = null) : AggregateRoot(id)
{
    private readonly List<Batch> _batches = [];

    // ReSharper disable once UnusedMember.Local
    private Import() : this(string.Empty, Period.AllTime) { }

    // TODO: Add value objects for UserId and ArchiveJobId
    public string UserId { get; } = userId;
    public Period Period { get; private set; } = period ?? Period.AllTime;
    public string? ArchiveJobId { get; private set; }
    public ImportState State { get; private set; } = ImportState.Unspecified;
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    [NotMapped]
    public int Processed => _batches.Sum(x => x.StarredPlaces.Count(s => s.IsProcessed));

    [NotMapped]
    public int Created => _batches.Sum(x => x.StarredPlaces.Count(s => s.State == StarredPlaceState.New));

    [NotMapped]
    public int Updated => _batches.Sum(x => x.StarredPlaces.Count(s => s.State == StarredPlaceState.Exists));

    [NotMapped]
    public int Failed => FailedPlaces.Count;

    [NotMapped]
    public int Conflicts => ConflictedPlaces.Count;

    [NotMapped]
    public int Total => _batches.Sum(x => x.StarredPlaces.Count);

    public IReadOnlyCollection<Batch> Batches => _batches;

    [NotMapped]
    public IReadOnlyCollection<StarredPlace> ConflictedPlaces =>
        _batches.SelectMany(x => x.StarredPlaces.Where(s => s.State == StarredPlaceState.Conflicted)).ToList();

    [NotMapped]
    public IReadOnlyCollection<StarredPlace> FailedPlaces =>
        _batches.SelectMany(x => x.StarredPlaces.Where(s => s.State == StarredPlaceState.Invalid)).ToList();

    [NotMapped]
    public IReadOnlyDictionary<Guid, IReadOnlyCollection<StarredPlace>> BatchesMap =>
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

        if (_batches.Any(x => x.BatchThumbprint == batch.BatchThumbprint))
        {
            return Result.Ok();
        }

        _batches.Add(batch);

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

        if (!BatchesMap.TryGetValue(batchId, out var batch))
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

        Raise(new ImportBatchProcessed(Id, UserId, ArchiveJobId!, processed, created, updated, failed, conflicts, Total));

        return (toCreate, toUpdate);
    }

    public Result<Success> Complete()
    {
        if (State == ImportState.Complete)
        {
            return Result.Ok();
        }

        if (State != ImportState.InProgress)
        {
            return Result.Fail(Errors.Import.ImportNotInProgress(this));
        }

        if (_batches.SelectMany(x => x.StarredPlaces).Any(x => !x.IsProcessed))
        {
            return Result.Fail(Errors.Import.BatchesNotProcessed(this));
        }

        State = ImportState.Complete;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new ImportCompleted(Id, UserId, ArchiveJobId!));

        return Result.Ok();
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