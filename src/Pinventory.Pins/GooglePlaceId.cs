namespace Pinventory.Pins;

/// <summary>
/// Google Place ID value object.
/// Validates non-empty input and provides implicit conversions to/from string.
/// NOTE: Google does not publish a strict regex for Place IDs; they are stable opaque strings.
/// We enforce non-empty and a reasonable max length to catch obvious issues without over-restricting.
/// </summary>
public readonly struct GooglePlaceId : IEquatable<GooglePlaceId>
{
    public string Value { get; }

    public GooglePlaceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("GooglePlaceId is required", nameof(value));
        // Defensive upper bound; adjust if you decide to constrain length based on provider guidance
        if (value.Length > 512)
            throw new ArgumentOutOfRangeException(nameof(value), "GooglePlaceId is too long");

        Value = value.Trim();
    }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public override string ToString() => Value;

    // Equality semantics
    public bool Equals(GooglePlaceId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is GooglePlaceId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    // Implicit conversions for convenience in application/infrastructure layers
    public static implicit operator string(GooglePlaceId id) => id.Value;
    public static implicit operator GooglePlaceId(string value) => new GooglePlaceId(value);
}

