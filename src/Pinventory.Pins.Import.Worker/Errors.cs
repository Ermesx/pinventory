using System.Net;

using FluentResults;

namespace Pinventory.Pins.Import.Worker;

public static class Errors
{
    public static class ImportServiceFactory
    {
        public static Error NoTokensFoundForUser(string userId) => new($"No tokens found for user: {userId}");

        public static Error MissingDataPortabilityToken() => new("Data portability token is missing");
    }

    public static class GoogleArchiveProcessor
    {
        public static Error MissingService() => new("Missing service in archive browser");

        public static Error MissingExtractedFileMetadata() => new("Missing metadata for extracted file");

        public static Error FileNotFound(string filePath) => new($"Could not find '{filePath}' in the ZIP file");

        public static Error FileDeserializationFailed(string filePath) => new($"Failed to deserialize {Path.GetFileName(filePath)}");

        public static Error HttpRequestFailed(Uri uri, HttpStatusCode? statusCode) =>
            new($"HTTP request to '{uri}' failed with status code {statusCode}");

        public static Error InvalidContentType(Uri sourceUri) => new($"Content type is not application/zip for '{sourceUri}'");
    }

    public static class ImportService
    {
        public static Error ArchiveJobNotFound(string archiveJobId) => new($"Archive job not found: {archiveJobId}");

        public static Error CannotCancelJob(string archiveJobId) => new($"Cannot cancel job: {archiveJobId}");

        public static Error ArchiveJobAlreadyExists() => new Application.Errors.ImportHandler.ArchiveJobExists();

        public static Error UnexpectedError() => new("Unexpected error");
    }

    public static class GoogleStarredPlacesProvider
    {
        public static Error NotEnoughUrls() => new("Not enough URLs to download archive");
    }
}