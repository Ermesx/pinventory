namespace Pinventory.Pins.Import;

public enum ImportJobState
{
    Unspecified = 0,
    InProgress,
    Complete,
    Failed,
    Cancelled
}