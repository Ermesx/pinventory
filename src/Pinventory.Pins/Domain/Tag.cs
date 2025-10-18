using FluentResults;

namespace Pinventory.Pins.Domain;

public sealed record Tag
{
    public string Value { get; private set; }


    public static Result<Tag> Create(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? Result.Fail(Errors.Tag.TagCannotBeEmpty()) : new Tag(value);
    }

    private Tag(string value)
    {
        Value = value.Trim().ToLower();
    }
}