namespace Pinventory.Pins.Domain.Pins;

public sealed record Address
{
    public Address(string line)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line);
        this.Line = line;
    }

    public string Line { get; }
}