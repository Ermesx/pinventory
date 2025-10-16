namespace Pinventory.Pins.Domain.Tagging;

public sealed class TaggingJob
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public RunState State { get; private set; } = RunState.Started;

    public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; private set; }

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