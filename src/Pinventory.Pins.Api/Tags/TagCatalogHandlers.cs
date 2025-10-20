using FluentResults;

using Pinventory.Pins.Application.Tags;
using Pinventory.Pins.Application.Tags.Commands;

using Wolverine.Attributes;

namespace Pinventory.Pins.Api.Tags;

[WolverineHandler]
public static class TagCatalogHandlers
{
    public static async Task<Result<Guid>> Handle(DefineTagCatalogCommand command, TagCatalogHandler app) =>
        await app.Handle(command);

    public static async Task<Result<Success>> Handle(AddTagCommand command, TagCatalogHandler app) =>
        await app.Handle(command);

    public static async Task<Result<Success>> Handle(RemoveTagCommand command, TagCatalogHandler app) =>
        await app.Handle(command);
}