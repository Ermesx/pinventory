using FluentResults;

using Pinventory.Pins.Application.Tags.Commands;

namespace Pinventory.Pins.Application;

public static class Errors
{
    private const string GlobalUser = "global";

    public class NotFoundError(string message) : Error(message);

    public static class TagCatalogHandler
    {
        public static Error CatalogAlreadyExists(DefineTagCatalogCommand command) =>
            new($"Catalog already exists for user {GetOwner(command)}");

        public static Error CatalogNotFound(OwnerCommand command) => new NotFoundError($"Catalog not found for user {GetOwner(command)}");

        private static string? GetOwner(OwnerCommand command) => command.IsGlobal ? GlobalUser : command.OwnerId;
    }

    public static class Import
    {
        public static Error RunningImportNotFound(string userId, string archiveJobId) =>
            new NotFoundError($"Import {archiveJobId} not found for user {userId}");

        public class ArchiveJobExists() : Error("Archive job already exists")
        {
            public const string ArchiveJobIdMetadataKey = "ArchiveJobId";

            public string? ArchiveJobId => Metadata[ArchiveJobIdMetadataKey] as string;
        }
    }
}