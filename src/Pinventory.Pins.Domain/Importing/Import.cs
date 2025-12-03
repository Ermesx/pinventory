using System.ComponentModel.DataAnnotations.Schema;

using FluentResults;

using Pinventory.Pins.Domain.Abstractions;
using Pinventory.Pins.Domain.Importing.Events;

namespace Pinventory.Pins.Domain.Importing
{
    public sealed class Import(string userId, Period? period = null, Guid? id = null) : AggregateRoot(id)
    {
        private readonly HashSet<StarredPlace> _starredPlaces = new(new ImportStarredPlaceComparer());

        // ReSharper disable once UnusedMember.Local
        private Import() : this(string.Empty, Period.AllTime) { }

        // TODO: Add value objects for UserId and ArchiveJobId and use init instead of constructor
        public string UserId { get; } = userId;
        public Period Period { get; } = period ?? Period.AllTime;
        public string? ArchiveJobId { get; private set; }
        public ImportState State { get; private set; } = ImportState.Unspecified;
        public DateTimeOffset? StartedAt { get; private set; }
        public DateTimeOffset? CompletedAt { get; private set; }

        public IReadOnlyCollection<StarredPlace> StarredPlaces => _starredPlaces;

        [NotMapped]
        public int Processed => _starredPlaces.Count(s => s.IsProcessed);

        [NotMapped]
        public int Created => _starredPlaces.Count(s => s.State == StarredPlaceState.New);

        [NotMapped]
        public int Updated => _starredPlaces.Count(s => s.State == StarredPlaceState.Exists);

        [NotMapped]
        public int Failed => FailedPlaces.Count;

        [NotMapped]
        public int Conflicts => ConflictedPlaces.Count;

        [NotMapped]
        public int Total => _starredPlaces.Count;

        [NotMapped]
        public IReadOnlyCollection<StarredPlace> ConflictedPlaces =>
            _starredPlaces.Where(s => s.State == StarredPlaceState.Conflicted).ToList();

        [NotMapped]
        public IReadOnlyCollection<StarredPlace> FailedPlaces =>
            _starredPlaces.Where(s => s.State == StarredPlaceState.Invalid).ToList();

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

        public Result<Success> RegisterPlaces(IReadOnlyList<StarredPlace> places)
        {
            if (State != ImportState.InProgress)
            {
                return Result.Fail(Errors.Import.CannotRegisterPlaces(State));
            }

            var validPlaces = places.Where(IsInPeriod).ToList();
            if (validPlaces.Count == 0)
            {
                return Result.Fail(Errors.Import.PlacesCannotBeEmpty());
            }

            foreach (var place in validPlaces)
            {
                if (_starredPlaces.Add(place))
                {
                    Raise(new ImportPlaceRegistered(Id, UserId, ArchiveJobId!, place.Id));
                }
            }

            return Result.Ok();

            bool IsInPeriod(StarredPlace place) => place.AddedDate >= Period.Start && place.AddedDate <= Period.End;
        }

        public async Task<Result<(IReadOnlyList<StarredPlace> ToCreate, IReadOnlyList<StarredPlace> ToUpdate)>> ProcessPlacesAsync(
            IReadOnlySet<Guid> placeIds,
            IStarredPlaceValidator validator, CancellationToken cancellationToken = default)
        {
            if (State != ImportState.InProgress)
            {
                return Result.Fail(Errors.Import.ImportNotInProgress(this));
            }

            var placesToProcess = _starredPlaces.Where(x => !x.IsProcessed && placeIds.Contains(x.Id)).ToList();
            if (placesToProcess.Count == 0)
            {
                return Result.Fail(Errors.Import.ThereIsNoPlacesToProcess(this));
            }

            var toCreate = new List<StarredPlace>();
            var toUpdate = new List<StarredPlace>();
            foreach (var place in placesToProcess)
            {
                place.State = await validator.ValidateAsync(this, place, cancellationToken);
                place.IsProcessed = true;
                if (place.State == StarredPlaceState.New)
                {
                    toCreate.Add(place);
                }
                else if (place.State == StarredPlaceState.Exists)
                {
                    toUpdate.Add(place);
                }

                Raise(new ImportPlaceProcessed(Id, UserId, ArchiveJobId!, place.Id, place.State));
            }

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

            if (_starredPlaces.Any(x => !x.IsProcessed))
            {
                return Result.Fail(Errors.Import.PlacesNotProcessed(this));
            }

            State = ImportState.Complete;
            CompletedAt = DateTimeOffset.UtcNow;
            Raise(new ImportCompleted(Id, UserId, ArchiveJobId!));

            return Result.Ok();
        }

        public Result<Success> ClearPlaces()
        {
            if (State != ImportState.InProgress)
            {
                return Result.Fail(Errors.Import.ImportNotInProgress(this));
            }

            _starredPlaces.Clear();
            Raise(new ImportPlacesCleared(Id, UserId, ArchiveJobId!));

            return Result.Ok();
        }

        public Result<Success> Fail(IError error)
        {
            if (State == ImportState.Failed)
            {
                return Result.Ok();
            }

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
            if (State == ImportState.Cancelled)
            {
                return Result.Ok();
            }

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
}