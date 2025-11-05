using FluentResults;

namespace Pinventory.Pins.Domain;

public sealed record Tag
{
    private Tag(string value)
    {
        Value = value.Trim().ToLower();
    }

    public string Value { get; }

    public static Result<Tag> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result.Fail(Errors.Tag.TagCannotBeEmpty())
            : new Tag(value);
}