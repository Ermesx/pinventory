namespace Pinventory.Pins.Domain.Importing;

public enum ImportState
{
    Unspecified = 0,
    InProgress,
    Complete,
    Failed,
    Cancelled
}