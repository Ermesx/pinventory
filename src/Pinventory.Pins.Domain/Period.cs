using FluentResults;

namespace Pinventory.Pins.Domain;

// TODO: Maybe replace with NodaTime 
public sealed record Period
{
    private Period(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    public static Period AllTime => new(DateTimeOffset.MinValue, DateTimeOffset.UtcNow);

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public static Result<Period> Create(DateTimeOffset start, DateTimeOffset end) =>
        start >= end
            ? Result.Fail<Period>("Start must be earlier than end")
            : new Period(start, end);
}