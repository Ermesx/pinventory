using FluentResults;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Api.Tags.Dtos;
using Pinventory.Pins.Application;
using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.Pins.Infrastructure;

using Wolverine;

namespace Pinventory.Pins.Api.Tags;

public static class Endpoints
{
    public static WebApplication MapTagsEndpoints(this WebApplication app)
    {
        var tagsEndpoint = app.MapGroup("/tags")
            .RequireAuthorization("OwnerMatchesUser")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();

        // Get Tags from a catalog
        tagsEndpoint.MapGet("{ownerId}",
                async (string? ownerId, [FromServices] PinsDbContext dbContext) =>
                {
                    var tags = await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == ownerId);
                    return tags is null
                        ? Results.NotFound()
                        : Results.Ok(new TagCatalogDto(tags.Tags.Select(t => t.Value)));
                }).WithName("GetTags")
            .Produces<TagCatalogDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);


        // Define TagCatalog
        tagsEndpoint.MapPost("/{ownerId}/define",
                async (string? ownerId, [FromBody] TagsDto request, [FromServices] IMessageBus bus) =>
                {
                    var command = new DefineTagCatalogCommand(ownerId, request.Tags);
                    var result = await bus.InvokeAsync<Result<Guid>>(command);
                    return result.IsSuccess
                        ? Results.Created($"/tags/{command.OwnerId}", new TagCatalogIdDto(ownerId, result.Value))
                        : Results.Conflict(result.Errors);
                }).WithName("DefineTags")
            .Produces<TagCatalogIdDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Add Tag to catalog
        tagsEndpoint.MapPut("/{ownerId}",
                async (string? ownerId, [FromBody] TagDto request, [FromServices] IMessageBus bus) =>
                {
                    var command = new AddTagCommand(ownerId, request.Tag);
                    var result = await bus.InvokeAsync<Result<Success>>(command);
                    if (result.IsSuccess)
                    {
                        return Results.Created();
                    }

                    var error = result.Errors.Single();
                    return error is Errors.NotFoundError
                        ? Results.NotFound(error.Message)
                        : Results.BadRequest(error.Message);
                }).WithName("AddTag")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Remove Tag from catalog
        tagsEndpoint.MapDelete("/{ownerId}",
                async (string? ownerId, [FromBody] TagDto request, [FromServices] IMessageBus bus) =>
                {
                    var command = new RemoveTagCommand(ownerId, request.Tag);
                    var result = await bus.InvokeAsync<Result<Success>>(command);
                    if (result.IsSuccess)
                    {
                        return Results.NoContent();
                    }

                    var error = result.Errors.Single();
                    return error is Errors.NotFoundError
                        ? Results.NotFound(error.Message)
                        : Results.BadRequest(error.Message);
                }).WithName("RemoveTag")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}