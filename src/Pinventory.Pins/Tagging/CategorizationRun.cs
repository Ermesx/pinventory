namespace Pinventory.Pins.Tagging;

public enum RunState { Started = 0, Completed = 1, Failed = 2 }

public sealed class TaggingJob
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public RunState State { get; private set; } = RunState.Started;

    public string ModelVersion { get; private set; } = default!;
    public string Scope { get; private set; } = default!;
    public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; private set; }

    private TaggingJob() { }

    public static TaggingJob Start(string scope, string modelVersion)
        => new() { Scope = scope, ModelVersion = modelVersion };

    public void Complete()
    {
        State = RunState.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string error)
    {
        State = RunState.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}