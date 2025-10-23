namespace Pinventory.Pins.Application.Tags.Commands;

public record DefineTagCatalogCommand(string? OwnerId, IEnumerable<string> Tags) : OwnerCommand(OwnerId);