using FluentResults;

using Pinventory.Pins.Application.Tags;
using Pinventory.Pins.Application.Tags.Commands;

using Wolverine.Attributes;

namespace Pinventory.Pins.Api.Tags;

[WolverineHandler]
public static class TagCatalogHandlers
{
    public static async Task<Result<Guid>> HandleAsync(DefineTagCatalogCommand command, TagCatalogHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(command, cancellationToken);

    public static async Task<Result<Success>> HandleAsync(AddTagCommand command, TagCatalogHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(command, cancellationToken);

    public static async Task<Result<Success>> HandleAsync(RemoveTagCommand command, TagCatalogHandler app,
        CancellationToken cancellationToken = default) =>
        await app.HandleAsync(command, cancellationToken);
}