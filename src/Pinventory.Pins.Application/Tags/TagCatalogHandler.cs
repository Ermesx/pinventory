using FluentResults;

using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Application.Abstractions;
using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.Pins.Domain.Tags;
using Pinventory.Pins.Infrastructure;

using Wolverine;

namespace Pinventory.Pins.Application.Tags;

// dbContext.SaveChangesAsync() is not use because Wolverine handles transactional outbox 
public sealed class TagCatalogHandler(PinsDbContext dbContext, IMessageBus bus) : ApplicationHandler(bus)
{
    public async Task<Result<Guid>> Handle(DefineTagCatalogCommand command)
    {
        var tagsCatalog = await GetTagCatalog(command);
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
        await RaiseEvents(tagsCatalog);

        return Result.Ok(tagsCatalog.Id);
    }

    public async Task<Result<Success>> Handle(AddTagCommand command)
    {
        var tagCatalog = await GetTagCatalog(command);
        if (tagCatalog is null)
        {
            return Result.Fail(Errors.TagCatalogHandler.CatalogNotFound(command));
        }

        var result = tagCatalog.AddTag(command.Tag);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        await RaiseEvents(tagCatalog);

        return Result.Ok();
    }

    public async Task<Result<Success>> Handle(RemoveTagCommand command)
    {
        var tagCatalog = await GetTagCatalog(command);
        if (tagCatalog is null)
        {
            return Result.Fail(Errors.TagCatalogHandler.CatalogNotFound(command));
        }

        var result = tagCatalog.RemoveTag(command.Tag);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        await RaiseEvents(tagCatalog);

        return Result.Ok();
    }

    private async Task<TagCatalog?> GetTagCatalog(OwnerCommand command)
    {
        return await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == command.OwnerId);
    }
}