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

    public static Period AllTime => new(DateTimeOffset.UnixEpoch, DateTimeOffset.UtcNow);

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public static Result<Period> Create(DateTimeOffset? start, DateTimeOffset? end)
    {
        start ??= DateTimeOffset.UnixEpoch;
        end ??= DateTimeOffset.UtcNow;

        if (start < DateTimeOffset.UnixEpoch)
        {
            start = DateTimeOffset.UnixEpoch;
        }

        return start >= end
            ? Result.Fail<Period>(Errors.Period.PeriodStartMustBeBeforeEnd(start, end))
            : new Period(start.Value, end.Value);
    }
}