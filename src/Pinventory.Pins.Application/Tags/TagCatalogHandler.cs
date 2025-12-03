using FluentResults;

using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.Pins.Domain.Tags;

using Wolverine.Persistence;

namespace Pinventory.Pins.Application.Tags;

public sealed class TagCatalogHandler(ILogger<TagCatalogHandler> logger)
{
    public (Result<Guid> Result, IStorageAction<TagCatalog> Storage) Handle(DefineTagCatalogCommand command, TagCatalog? tagCatalog)
    {
        logger.LogInformation("Defining tag catalog for {OwnerId}", command.OwnerId);
        if (tagCatalog is not null)
        {
            return (Result.Fail(Errors.TagCatalogHandler.CatalogAlreadyExists(command)), Storage.Nothing<TagCatalog>());
        }

        tagCatalog = new TagCatalog(command.OwnerId);
        var result = tagCatalog.DefineTags(command.Tags);
        if (result.IsFailed)
        {
            return (Result.Fail(result.Errors), Storage.Nothing<TagCatalog>());
        }

        return (Result.Ok(tagCatalog.Id), Storage.Insert(tagCatalog));
    }

    public Result<Success> Handle(AddTagCommand command, TagCatalog? tagCatalog)
    {
        logger.LogInformation("Adding tag {Tag} to catalog for {OwnerId}", command.Tag, command.OwnerId);
        if (tagCatalog is null)
        {
            return Result.Fail(Errors.TagCatalogHandler.CatalogNotFound(command));
        }

        var result = tagCatalog.AddTag(command.Tag);
        return result.IsFailed
            ? Result.Fail(result.Errors)
            : Result.Ok();
    }

    public Result<Success> Handle(RemoveTagCommand command, TagCatalog? tagCatalog)
    {
        logger.LogInformation("Removing tag {Tag} from catalog for {OwnerId}", command.Tag, command.OwnerId);
        if (tagCatalog is null)
        {
            return Result.Fail(Errors.TagCatalogHandler.CatalogNotFound(command));
        }

        var result = tagCatalog.RemoveTag(command.Tag);
        return result.IsFailed
            ? Result.Fail(result.Errors)
            : Result.Ok();
    }
}