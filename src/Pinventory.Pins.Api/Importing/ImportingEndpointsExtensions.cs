using System.Security.Claims;

using JasperFx.Core;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Pinventory.ApiDefaults;
using Pinventory.Pins.Api.Importing.Dtos;
using Pinventory.Pins.Api.Importing.Realtime;
using Pinventory.Pins.Application.Abstractions.Results;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure;

using Wolverine;

using Errors = Pinventory.Pins.Application.Errors;

namespace Pinventory.Pins.Api.Importing;

public static class ImportingEndpointsExtensions
{
    private static readonly TimeSpan CommandsTimeout = 30.Seconds();

    public static WebApplication MapImportingEndpoints(this WebApplication app)
    {
        var importsEndpoint = app.MapGroup("/imports")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        importsEndpoint.MapGet("/", GetImports)
            .WithName("GetImports")
            .Produces<List<ImportDto>>();

        importsEndpoint.MapGet("/{archiveJobId}", GetImport)
            .WithName("GetImport")
            .Produces<ImportDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        importsEndpoint.MapPost("/", StartImport)
            .WithName("StartImport")
            .Produces<string>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        importsEndpoint.MapPost("/{archiveJobId}/cancel", CancelImport)
            .WithName("CancelImport")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        // Add SignalR hub
        app.MapHub<ImportProgressHub>("/hubs/imports").RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetImports(ClaimsPrincipal user, [FromServices] PinsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = user.GetIdentifier();
        var imports = await dbContext.Imports
            .Where(x => x.UserId == userId)
            .Select(x => new ImportDto(
                x.Id,
                x.ArchiveJobId,
                x.State,
                x.StartedAt,
                x.CompletedAt,
                x.Processed,
                x.Created,
                x.Updated,
                x.Failed,
                x.Conflicts,
                x.Total,
                x.ConflictedPlaces.Select<ReportedPlace, (string MapsUrl, DateTimeOffset AddedDate)>(p => new(p.MapsUrl, p.AddedDate)),
                x.FailedPlaces.Select<ReportedPlace, (string MapsUrl, DateTimeOffset AddedDate)>(p => new(p.MapsUrl, p.AddedDate))))
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken);

        return Results.Ok(imports);
    }

    private static async Task<IResult> GetImport(string archiveJobId, ClaimsPrincipal user, [FromServices] PinsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = user.GetIdentifier();
        var import = await dbContext.Imports
            .Where(x => x.UserId == userId && x.ArchiveJobId == archiveJobId)
            .Select(x => new ImportDto(
                x.Id,
                x.ArchiveJobId,
                x.State,
                x.StartedAt,
                x.CompletedAt,
                x.Processed,
                x.Created,
                x.Updated,
                x.Failed,
                x.Conflicts,
                x.Total,
                x.ConflictedPlaces.Select<ReportedPlace, (string MapsUrl, DateTimeOffset AddedDate)>(p => new(p.MapsUrl, p.AddedDate)),
                x.FailedPlaces.Select<ReportedPlace, (string MapsUrl, DateTimeOffset AddedDate)>(p => new(p.MapsUrl, p.AddedDate))))
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        return import is null
            ? Results.NotFound()
            : Results.Ok(import);
    }

    private static async Task<IResult> StartImport(ClaimsPrincipal user, [FromBody] StartImportDto request, [FromServices] IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var userId = user.GetIdentifier();

        var archiveJobIdResult =
            await bus.InvokeAsync<ResultDto<string>>(new StartImportCommand(userId, request.Start, request.End), cancellationToken,
                CommandsTimeout);

        return archiveJobIdResult.IsSuccess
            ? Results.Created($"/imports/{archiveJobIdResult.Value}", archiveJobIdResult.Value)
            : archiveJobIdResult.HasError<Domain.Errors.Period.IncorrectPeriodDates>()
                ? Results.BadRequest(archiveJobIdResult.Errors)
                : Results.Conflict(archiveJobIdResult.Errors);
    }

    private static async Task<IResult> CancelImport(string archiveJobId, ClaimsPrincipal user, [FromServices] IMessageBus bus,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(archiveJobId))
        {
            return Results.BadRequest();
        }

        var userId = user.GetIdentifier();
        var result = await bus.InvokeAsync<ResultDto>(new CancelImportCommand(userId, archiveJobId), cancellationToken, CommandsTimeout);

        return result.IsSuccess
            ? Results.Ok()
            : result.HasError<Errors.NotFoundError>()
                ? Results.NotFound(result.Errors)
                : Results.Conflict(result.Errors);
    }
}