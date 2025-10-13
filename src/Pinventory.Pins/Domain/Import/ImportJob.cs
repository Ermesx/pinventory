namespace Pinventory.Pins.Domain.Import;

public sealed class ImportJob
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }

    public ImportJobState State { get; private set; }

    public string Source { get; private set; } = default!;

    public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; private set; }
    public int Processed { get; private set; }
    public int Created { get; private set; }
    public int Updated { get; private set; }
    public int Failed { get; private set; }
    public int Conflicts { get; private set; }
    public string? Cursor { get; private set; }

    private ImportJob() { }

    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents;
    /// <summary>
    /// IMPORTANT: Ensure <see cref="IImportConcurrencyPolicy"/> is checked before calling <see cref="Start"/>,
    /// so that only one <see cref="ImportJobState.InProgress"/> job exists per user.
    /// </summary>

    private void Raise(object @event) => _domainEvents.Add(@event);

    public static ImportJob Start(Guid userId, string source, string? cursor)
        => new() { UserId = userId, Source = source, Cursor = cursor, State = ImportJobState.InProgress };

    public void Started()
    {
        if (State != ImportJobState.InProgress) throw new InvalidOperationException("Import already started or not in progress");
        Raise(new Events.ImportStarted(Id, UserId, Source));
    }

    public void AppendBatch(int processed, int created, int updated, int failed, int conflicts)
    {
        if (State != ImportJobState.InProgress) throw new InvalidOperationException("Import is not in progress");
        if (processed < 0 || created < 0 || updated < 0 || failed < 0 || conflicts < 0)
            throw new ArgumentOutOfRangeException("Batch counters must be non-negative");
        Processed += processed;
        Created += created;
        Updated += updated;
        Failed += failed;
        Conflicts += conflicts;
        Raise(new Events.ImportBatchProcessed(Id, processed, created, updated, failed, conflicts));
    }

    public void Complete()
    {
        if (State != ImportJobState.InProgress) throw new InvalidOperationException("Import is not in progress");
        State = ImportJobState.Complete;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new Events.ImportCompleted(Id));
    }

    public void Fail(string error)
    {
        if (State != ImportJobState.InProgress) throw new InvalidOperationException("Import is not in progress");
        if (string.IsNullOrWhiteSpace(error)) throw new ArgumentException("Error is required", nameof(error));
        State = ImportJobState.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        Raise(new Events.ImportFailed(Id, error));
    }
}