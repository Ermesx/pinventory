using FluentResults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.Pins.Domain.Tags;
using Pinventory.Pins.Infrastructure;

using Wolverine;

namespace Pinventory.Pins.Application.Tags;

// dbContext.SaveChangesAsync() is not used because Wolverine handles transactional outbox 
public sealed class TagCatalogHandler(ILogger<TagCatalogHandler> logger, PinsDbContext dbContext, IMessageBus bus)
    : ApplicationHandler(bus)
{
    public async Task<Result<Guid>> HandleAsync(DefineTagCatalogCommand command)
    {
        logger.LogInformation("Defining tag catalog for {OwnerId}", command.OwnerId);

        var tagsCatalog = await GetTagCatalogAsync(command);
        if (tagsCatalog is not null)
        {
            return Result.Fail(Errors.TagCatalogHandler.CatalogAlreadyExists(command));
        }

        tagsCatalog = new TagCatalog(command.OwnerId);
        var result = tagsCatalog.DefineTags(command.Tags);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        await dbContext.TagCatalogs.AddAsync(tagsCatalog);
        await RaiseEventsAsync(tagsCatalog);

        return Result.Ok(tagsCatalog.Id);
    }

    public async Task<Result<Success>> HandleAsync(AddTagCommand command)
    {
        logger.LogInformation("Adding tag {Tag} to catalog for {OwnerId}", command.Tag, command.OwnerId);

        var tagCatalog = await GetTagCatalogAsync(command);
        if (tagCatalog is null)
        {
            return Result.Fail(Errors.TagCatalogHandler.CatalogNotFound(command));
        }

        var result = tagCatalog.AddTag(command.Tag);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        await RaiseEventsAsync(tagCatalog);

        return Result.Ok();
    }

    public async Task<Result<Success>> HandleAsync(RemoveTagCommand command)
    {
        logger.LogInformation("Removing tag {Tag} from catalog for {OwnerId}", command.Tag, command.OwnerId);

        var tagCatalog = await GetTagCatalogAsync(command);
        if (tagCatalog is null)
        {
            return Result.Fail(Errors.TagCatalogHandler.CatalogNotFound(command));
        }

        var result = tagCatalog.RemoveTag(command.Tag);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        await RaiseEventsAsync(tagCatalog);

        return Result.Ok();
    }

    private async Task<TagCatalog?> GetTagCatalogAsync(OwnerCommand command)
    {
        return await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == command.OwnerId);
    }
}