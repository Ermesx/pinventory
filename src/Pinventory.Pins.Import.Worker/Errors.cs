using FluentResults;

namespace Pinventory.Pins.Import.Worker;

public static class Errors
{
    public static class ImportServiceFactory
    {
        public static Error NoTokensFoundForUser(string userId) => new($"No tokens found for user: {userId}");

        public static Error MissingDataPortabilityToken() => new("Data portability token is missing");
    }
}