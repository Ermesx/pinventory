using FluentResults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.Pins.Domain.Tags;
using Pinventory.Pins.Infrastructure;

namespace Pinventory.Pins.Application.Tags;

// dbContext.SaveChangesAsync() is not used because Wolverine handles transactional outbox 
// FluentResults can be used because this handler is used as internal MediatR
public sealed class TagCatalogHandler(ILogger<TagCatalogHandler> logger, PinsDbContext dbContext)
{
    public async Task<Result<Guid>> HandleAsync(DefineTagCatalogCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Defining tag catalog for {OwnerId}", command.OwnerId);

        var tagsCatalog = await GetTagCatalogAsync(command, cancellationToken);
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

        await dbContext.TagCatalogs.AddAsync(tagsCatalog, cancellationToken);

        return Result.Ok(tagsCatalog.Id);
    }

    public async Task<Result<Success>> HandleAsync(AddTagCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding tag {Tag} to catalog for {OwnerId}", command.Tag, command.OwnerId);

        var tagCatalog = await GetTagCatalogAsync(command, cancellationToken);
        if (tagCatalog is null)
        {
            return Result.Fail(Errors.TagCatalogHandler.CatalogNotFound(command));
        }

        var result = tagCatalog.AddTag(command.Tag);
        return result.IsFailed
            ? Result.Fail(result.Errors)
            : Result.Ok();
    }

    public async Task<Result<Success>> HandleAsync(RemoveTagCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Removing tag {Tag} from catalog for {OwnerId}", command.Tag, command.OwnerId);

        var tagCatalog = await GetTagCatalogAsync(command, cancellationToken);
        if (tagCatalog is null)
        {
            return Result.Fail(Errors.TagCatalogHandler.CatalogNotFound(command));
        }

        var result = tagCatalog.RemoveTag(command.Tag);
        return result.IsFailed
            ? Result.Fail(result.Errors)
            : Result.Ok();
    }

    private async Task<TagCatalog?> GetTagCatalogAsync(OwnerCommand command, CancellationToken cancellationToken)
    {
        return await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == command.OwnerId, cancellationToken);
    }
}