using FluentResults;

namespace Pinventory.Web;

public static class Errors
{
    public static class GoogleAuthState
    {
        public static Error StateMissing() => new("state_missing");
        public static Error StateInvalid() => new("state_invalid");
        public static Error StateMismatch() => new("state_mismatch");
        public static Error PropertiesMissing() => new("properties_missing");
    }
}