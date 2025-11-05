using FluentResults;

namespace Pinventory.Pins.Import.Worker;

public static class Errors
{
    public static class ImportServiceFactory
    {
        public static Error NoTokensFoundForUser(string userId) => new($"No tokens found for user: {userId}");

        public static Error MissingDataPortabilityToken() => new("Data portability token is missing");
    }

    public static class ArchiveDownload
    {
        public static Error MissingService() => new("Missing service in archive browser");

        public static Error MissingExtractedFileMetadata() => new("Missing metadata for extracted file");

        public static Error FileNotFound(string filePath) => new($"Could not find '{filePath}' in the ZIP file");

        public static Error FileDeserializationFailed(string filePath) => new($"Failed to deserialize {Path.GetFileName(filePath)}");

        public static Error HttpRequestFailed(HttpResponseMessage response) =>
            new(
                $"HTTP request to '{response.RequestMessage?.RequestUri}' failed with status code {response.StatusCode}: {response.ReasonPhrase}");
    }
}