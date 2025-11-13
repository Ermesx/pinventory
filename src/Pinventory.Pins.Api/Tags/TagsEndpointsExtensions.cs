using FluentResults;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Pinventory.ApiDefaults.Authorization;
using Pinventory.Pins.Api.Tags.Dtos;
using Pinventory.Pins.Application;
using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.Pins.Infrastructure;

using Wolverine;

namespace Pinventory.Pins.Api.Tags;

public static class TagsEndpointsExtensions
{
    public static WebApplication MapTagsEndpoints(this WebApplication app)
    {
        var tagsEndpoint = app.MapGroup("/tags")
            .RequireAuthorization(AuthPolicy.OwnerMatchesUser)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // Get Tags from a catalog
        tagsEndpoint.MapGet("/{ownerId?}", GetTags)
            .WithName("GetTags")
            .Produces<TagCatalogDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Define TagCatalog
        tagsEndpoint.MapPost("/{ownerId?}", DefineTags)
            .WithName("DefineTags")
            .Produces<TagCatalogIdDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Add Tag to catalog
        tagsEndpoint.MapPut("/{ownerId?}", AddTag)
            .WithName("AddTag")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        // Remove Tag from catalog
        tagsEndpoint.MapDelete("/{ownerId?}", RemoveTag)
            .WithName("RemoveTag")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> GetTags(string? ownerId, [FromServices] PinsDbContext dbContext, CancellationToken cancellationToken)
    {
        var catalog = await dbContext.TagCatalogs
            .Include(x => x.Tags)
            .Where(x => x.OwnerId == ownerId)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        return catalog is null
            ? Results.NotFound()
            : Results.Ok(new TagCatalogDto(catalog.Tags.Select(t => t.Value).ToList()));
    }

    private static async Task<IResult> DefineTags(string? ownerId, [FromBody] TagsDto request, [FromServices] IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var command = new DefineTagCatalogCommand(ownerId, request.Tags);
        var result = await bus.InvokeAsync<Result<Guid>>(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/tags/{command.OwnerId}", new TagCatalogIdDto(ownerId, result.Value))
            : Results.Conflict(result.Errors);
    }

    private static async Task<IResult> AddTag(string? ownerId, [FromBody] TagDto request, [FromServices] IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var command = new AddTagCommand(ownerId, request.Tag);
        var result = await bus.InvokeAsync<Result<Success>>(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created()
            : result.HasError<Errors.NotFoundError>()
                ? Results.NotFound(result.Errors)
                : Results.BadRequest(result.Errors);
    }

    private static async Task<IResult> RemoveTag(string? ownerId, [FromBody] TagDto request, [FromServices] IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var command = new RemoveTagCommand(ownerId, request.Tag);
        var result = await bus.InvokeAsync<Result<Success>>(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.HasError<Errors.NotFoundError>()
                ? Results.NotFound(result.Errors)
                : Results.BadRequest(result.Errors);
    }
}