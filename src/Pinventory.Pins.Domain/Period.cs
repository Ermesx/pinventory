using FluentResults;

namespace Pinventory.Pins.Domain;

// TODO: Maybe replace with NodaTime 
public partial record Period
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

        return start >= end
            ? Result.Fail<Period>(Errors.Period.PeriodStartMustBeBeforeEnd(start, end))
            : new Period(start.Value, end.Value);
    }
}