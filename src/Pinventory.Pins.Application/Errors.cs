using FluentResults;

using Pinventory.Pins.Application.Tags.Commands;

namespace Pinventory.Pins.Application;

public static class Errors
{
    private const string GlobalUser = "global";

    public static class TagCatalogHandler
    {
        public static Error CatalogAlreadyExists(DefineTagCatalogCommand command) =>
            new($"Catalog already exists for user {GetOwner(command)}");

        public static Error CatalogNotFound(OwnerCommand command) => new NotFoundError($"Catalog not found for user {GetOwner(command)}");

        private static string? GetOwner(OwnerCommand command) => command.IsGlobal ? GlobalUser : command.OwnerUserId.ToString();
    }

    public class NotFoundError(string message) : Error(message);
}