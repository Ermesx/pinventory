using FluentResults;

using Pinventory.Pins.Application.Tags;
using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.Pins.Domain.Tags;

using Wolverine;
using Wolverine.Attributes;
using Wolverine.Persistence;

namespace Pinventory.Pins.Api.Tags;

[WolverineHandler]
public static class TagCatalogHandlers
{
    public static (Result<Guid>, IStorageAction<TagCatalog>) Handle(DefineTagCatalogCommand command, TagCatalog? tagCatalog,
        TagCatalogHandler app) =>
        app.Handle(command, tagCatalog);

    public static Result<Success> Handle(AddTagCommand command, TagCatalog? tagCatalog, TagCatalogHandler app) =>
        app.Handle(command, tagCatalog);

    public static Result<Success> Handle(RemoveTagCommand command, TagCatalog? tagCatalog, TagCatalogHandler app) =>
        app.Handle(command, tagCatalog);

    public static void RouteTagCatalogCommands(this WolverineOptions options)
    {
        options.PublishMessage<DefineTagCatalogCommand>().Locally();
        options.PublishMessage<AddTagCommand>().Locally();
        options.PublishMessage<RemoveTagCommand>().Locally();
    }
}