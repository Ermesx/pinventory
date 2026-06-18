using System.Text.RegularExpressions;

using FluentResults;

namespace Pinventory.Pins.Domain;

public sealed partial record Tag
{
    public const int MaxLength = 50;

    [GeneratedRegex("^[a-zA-Z0-9\\s\\-_]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidFormat();

    private Tag(string value) => Value = value;

    public string Value { get; }

    public static Result<Tag> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Fail(Errors.Tag.TagCannotBeEmpty());
        }

        var normalized = value.Trim();
        if (normalized.Length > MaxLength)
        {
            return Result.Fail(Errors.Tag.TagTooLong(MaxLength));
        }

        if (!ValidFormat().IsMatch(normalized))
        {
            return Result.Fail(Errors.Tag.TagInvalidFormat());
        }

        return new Tag(normalized.ToLower());
    }
}