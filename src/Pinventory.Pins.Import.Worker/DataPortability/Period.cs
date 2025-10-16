namespace Pinventory.Pins.Import.Worker.DataPortability;

public record Period
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public Period(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
        {
            throw new ArgumentException("Start must be earlier than end", nameof(start));
        }

        Start = start;
        End = end;
    }
}