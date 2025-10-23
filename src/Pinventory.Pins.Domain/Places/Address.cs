namespace Pinventory.Pins.Domain.Places;

public sealed record Address
{
    public static readonly Address Unknown = new("-");

    public Address(string line)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line);
        this.Line = line;
    }

    public string Line { get; }
}