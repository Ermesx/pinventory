namespace Pinventory.Pins.Verification;

public enum RunState { Started = 0, Completed = 1, Failed = 2 }

public sealed class VerificationJob
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public RunState State { get; private set; } = RunState.Started;

    public string Scope { get; private set; } = default!;
    public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; private set; }

    private VerificationJob() { }
    public static VerificationJob Start(string scope) => new() { Scope = scope, State = RunState.Started };

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