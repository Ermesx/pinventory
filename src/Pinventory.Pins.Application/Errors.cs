using FluentResults;

using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.Pins.Infrastructure.Messages;

namespace Pinventory.Pins.Application;

public static class Errors
{
    private const string GlobalUser = "global";

    public class NotFoundError(string message) : Error(message);

    public static class TagCatalogHandler
    {
        public static Error CatalogAlreadyExists(DefineTagCatalogCommand command) =>
            new($"Catalog already exists for user {GetOwner(command)}");

        public static Error CatalogNotFound(IOwnerCommand command) => new NotFoundError($"Catalog not found for user {GetOwner(command)}");

        private static string? GetOwner(IOwnerCommand command) => command.IsGlobal ? GlobalUser : command.OwnerId;
    }

    public static class ImportHandler
    {
        public static Error RunningImportNotFound(string userId, Guid importId) =>
            new NotFoundError($"Import {importId} not found for user {userId}");

        public static Error ExternalJobFailed() => new("Archive job failed externally");

        public class ArchiveJobExists() : Error("Archive job already exists")
        {
            public const string ArchiveJobIdMetadataKey = "ArchiveJobId";

            public string? ArchiveJobId => Metadata[ArchiveJobIdMetadataKey] as string;
        }
    }
}