using FluentResults;

using Pinventory.Pins.Application.Tags;
using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.ServiceDefaults.Wolverine;

using Wolverine;
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

    public static void RouteTagCatalogCommandsLocally(this WolverineOptions options)
    {
        options.PublishMessage<DefineTagCatalogCommand>().LocalMessage<DefineTagCatalogCommand>();
        options.PublishMessage<AddTagCommand>().LocalMessage<AddTagCommand>();
        options.PublishMessage<RemoveTagCommand>().LocalMessage<RemoveTagCommand>();
    }
}