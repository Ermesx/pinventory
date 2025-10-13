using FluentResults;

namespace Pinventory.Pins.Import.Worker;

public static class Errors
{
    public static class ImportServiceFactory
    {
        public static Error NoTokensFoundForUser(string userId) => new($"No tokens found for user: {userId}");
    }
}