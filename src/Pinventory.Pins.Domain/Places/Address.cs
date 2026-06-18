using FluentResults;

using Nager.Country;

namespace Pinventory.Pins.Domain.Places;

public sealed record Address
{
    public static readonly Address Unknown = new("-", Alpha2Code.PL);

    private Address(string line, Alpha2Code countryCode)
    {
        Line = line;
        CountryCode = countryCode;
    }

    public string Line { get; }

    public Alpha2Code CountryCode { get; }

    public static Result<Address> Create(string line, Alpha2Code countryCode)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return Result.Fail(Errors.Address.AddressLineCannotBeEmpty());
        }

        return new Address(line.Trim(), countryCode);
    }

    public static Address From(string line, Alpha2Code countryCode) => new(line, countryCode);
}