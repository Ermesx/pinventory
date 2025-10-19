using FluentResults;

using Pinventory.Pins.Application.Tags.Commands;

namespace Pinventory.Pins.Application;

public static class Errors
{
    private const string GlobalUser = "global";

    public static class TagCatalogHandler
    {
        public static Error CatalogAlreadyExists(DefineTagCatalogCommand command) =>
            new($"Catalog already exists for user {(command.IsGlobal ? GlobalUser : command.OwnerUserId)}");

        public static Error CatalogNotFound(OwnerCommand command) => new($"Catalog not found for user {command.OwnerUserId}");
    }
}