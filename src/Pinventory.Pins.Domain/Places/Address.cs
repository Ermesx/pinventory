using Nager.Country;

namespace Pinventory.Pins.Domain.Places;

public sealed record Address
{
    public static readonly Address Unknown = new("-", Alpha2Code.PL);

    public Address(string line, Alpha2Code countryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line);
        this.Line = line;
        this.CountryCode = countryCode;
    }

    public string Line { get; }

    public Alpha2Code CountryCode { get; }
}