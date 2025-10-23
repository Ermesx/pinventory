using FluentResults;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Pinventory.ApiDefaults;
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
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();

        // Get Tags from a catalog
        tagsEndpoint.MapGet("{ownerId}",
                async (string? ownerId, HttpContext context, [FromServices] PinsDbContext dbContext) =>
                {
                    if (ownerId is not null && ownerId != context.User.GetSub())
                    {
                        return Results.Unauthorized();
                    }

                    var tags = await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == ownerId);
                    return Results.Ok(new TagCatalogDto(tags?.Tags.Select(t => t.Value) ?? []!));
                }).WithName("GetTags")
            .Produces<TagCatalogDto>();


        // Define TagCatalog
        tagsEndpoint.MapPost("/{ownerId}/define",
                async (string? ownerId, [FromBody] IEnumerable<string> tags, HttpContext context, [FromServices] IMessageBus bus) =>
                {
                    if (ownerId is not null && ownerId != context.User.GetSub())
                    {
                        return Results.Unauthorized();
                    }

                    var command = new DefineTagCatalogCommand(ownerId, tags);
                    var result = await bus.InvokeAsync<Result<Guid>>(command);
                    return result.IsSuccess
                        ? Results.Created($"/tags/{command.OwnerId}", new TagCatalogIdDto(ownerId, result.Value))
                        : Results.Conflict(result.Errors);
                }).WithName("DefineTags")
            .Produces<TagCatalogIdDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Add Tag to catalog
        tagsEndpoint.MapPut("/{ownerId}",
                async (string? ownerId, [FromBody] string tag, HttpContext context, [FromServices] IMessageBus bus) =>
                {
                    if (ownerId is not null && ownerId != context.User.GetSub())
                    {
                        return Results.Unauthorized();
                    }
                    
                    var command = new AddTagCommand(ownerId, tag);
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
                async (string? ownerId, [FromBody] string tag, HttpContext context, [FromServices] IMessageBus bus) =>
                {
                    if (ownerId is not null && ownerId != context.User.GetSub())
                    {
                        return Results.Unauthorized();
                    }

                    var command = new RemoveTagCommand(ownerId, tag);
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