namespace Pinventory.Pins.Import;

public class PinsImportJob()
{
    private ImportJobState _state;
    private IEnumerable<string> _urls = [];

    public required Guid Id { get; init; }

    public required Guid UserId { get; init; }

    public required DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? Finished { get; private set; }

    public string? FinishedReason { get; private set; }

    public ImportJobState State
    {
        get { return _state; }
        set
        {
            if (_state is ImportJobState.Failed or ImportJobState.Cancelled or ImportJobState.Complete)
            {
                throw new InvalidOperationException("Cannot change state of finished job.");
            }

            _state = value;
        }
    }

    public void Fail(string reason)
    {
        _state = ImportJobState.Failed;
        Finished = DateTimeOffset.UtcNow;
        FinishedReason = reason;
        // produce event
    }

    public void Cancel()
    {
        _state = ImportJobState.Cancelled;
        FinishedReason = "Cancelled";

        // produce event 
    }

    public void Complete(IEnumerable<string> urls)
    {
        _state = ImportJobState.Complete;
        Finished = DateTimeOffset.UtcNow;
        FinishedReason = "Completed";

        _urls = urls;

        // produce event
    }
}