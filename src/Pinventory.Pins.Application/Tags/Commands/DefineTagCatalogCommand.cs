namespace Pinventory.Pins.Application.Tags.Commands;

public record DefineTagCatalogCommand(Guid? OwnerUserId, IEnumerable<string> Tags) : OwnerCommand(OwnerUserId);