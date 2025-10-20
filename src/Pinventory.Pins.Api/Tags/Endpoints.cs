using FluentResults;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        tagsEndpoint.MapGet("{ownerUserId:guid}", async (Guid? ownerUserId,
                [FromServices] PinsDbContext dbContext) =>
            {
                var tags = await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId);
                return Results.Ok(new TagCatalogDto(tags?.Tags.Select(t => t.Value) ?? []!));
            }).WithName("GetTags")
            .Produces<TagCatalogDto>();
            

        // Define TagCatalog
        tagsEndpoint.MapPost("/{ownerUserId:guid}/define", async (Guid? ownerUserId,
                [FromBody] IEnumerable<string> tags,
                [FromServices] IMessageBus bus) =>
            {
                var command = new DefineTagCatalogCommand(ownerUserId, tags);
                var result = await bus.InvokeAsync<Result<Guid>>(command);
                return result.IsSuccess
                    ? Results.Created($"/tags/{command.OwnerUserId}", new TagCatalogIdDto(ownerUserId, result.Value))
                    : Results.Conflict(result.Errors);
            }).WithName("DefineTags")
            .Produces<TagCatalogIdDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Add Tag to catalog
        tagsEndpoint.MapPut("/{ownerUserId:guid}", async (Guid? ownerUserId,
                [FromBody] string tag,
                [FromServices] IMessageBus bus) =>
            {
                var command = new AddTagCommand(ownerUserId, tag);
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
        tagsEndpoint.MapDelete("/{ownerUserId:guid}", async (Guid? ownerUserId,
                [FromBody] string tag,
                [FromServices] IMessageBus bus) =>
            {
                var command = new RemoveTagCommand(ownerUserId, tag);
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