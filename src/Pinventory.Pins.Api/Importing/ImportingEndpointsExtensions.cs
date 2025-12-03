using System.Security.Claims;

using JasperFx.Core;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Pinventory.ApiDefaults;
using Pinventory.Pins.Api.Importing.Dtos;
using Pinventory.Pins.Api.Importing.Realtime;
using Pinventory.Pins.Application;
using Pinventory.Pins.Application.Importing.Commands;
using Pinventory.Pins.Application.Results;
using Pinventory.Pins.Infrastructure;

using Wolverine;
using Wolverine.RabbitMQ;

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

        importsEndpoint.MapGet("/{importId}", GetImport)
            .WithName("GetImport")
            .Produces<ImportDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        importsEndpoint.MapPost("/", StartImport)
            .WithName("StartImport")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        importsEndpoint.MapPost("/{importId}/renew", RenewImport)
            .WithName("RenewImport")
            .Produces(StatusCodes.Status200OK)
            .Produces<Guid>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        importsEndpoint.MapPost("/{importId}/cancel", CancelImport)
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
        var imports = await dbContext.ImportSummaries
            .Where(x => x.UserId == userId)
            .Select(x => ImportDto.From(x))
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken);

        return Results.Ok(imports);
    }

    private static async Task<IResult> GetImport(Guid importId, ClaimsPrincipal user, [FromServices] PinsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = user.GetIdentifier();
        var import = await dbContext.ImportSummaries
            .Where(x => x.UserId == userId && x.Id == importId)
            .Select(x => ImportDto.From(x))
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        return import is null
            ? Results.NotFound()
            : Results.Ok(import);
    }

    private static async Task<IResult> StartImport(ClaimsPrincipal user, [FromBody] StartImportDto request, [FromServices] IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var userId = user.GetIdentifier();

        var importResult =
            await bus.InvokeAsync<ResultDto<Guid>>(new StartImportCommand(userId, request.Start, request.End), cancellationToken,
                CommandsTimeout);

        return importResult.IsSuccess
            ? Results.Created($"/imports/{importResult.Value}", importResult.Value)
            : importResult.HasError<Domain.Errors.Period.IncorrectPeriodDates>()
                ? Results.BadRequest(importResult.Errors)
                : Results.Conflict(importResult.Errors);
    }

    private static async Task<IResult> RenewImport(Guid? importId, ClaimsPrincipal user, [FromServices] IMessageBus bus,
        CancellationToken cancellationToken)
    {
        // TODO: in net 10 add required for import id instead of null check
        if (importId is null)
        {
            return Results.BadRequest();
        }

        var userId = user.GetIdentifier();
        var result = await bus.InvokeAsync<ResultDto>(new RenewImportCommand(userId, importId.Value), cancellationToken, CommandsTimeout);

        return result.IsSuccess
            ? Results.Ok()
            : result.HasError<Errors.NotFoundError>()
                ? Results.NotFound(result.Errors)
                : Results.Conflict(result.Errors);
    }

    private static async Task<IResult> CancelImport(Guid? importId, ClaimsPrincipal user, [FromServices] IMessageBus bus,
        CancellationToken cancellationToken)
    {
        if (importId is null)
        {
            return Results.BadRequest();
        }

        var userId = user.GetIdentifier();
        var result = await bus.InvokeAsync<ResultDto>(new CancelImportCommand(userId, importId.Value), cancellationToken, CommandsTimeout);

        return result.IsSuccess
            ? Results.Ok()
            : result.HasError<Errors.NotFoundError>()
                ? Results.NotFound(result.Errors)
                : Results.Conflict(result.Errors);
    }

    public static void RouteImportCommands(this WolverineOptions options)
    {
        options.PublishMessage<StartImportCommand>().ToRabbitQueue(PinsMessaging.QueueNames.ImportCommands);
        options.PublishMessage<RenewImportCommand>().ToRabbitQueue(PinsMessaging.QueueNames.ImportCommands);
        options.PublishMessage<CancelImportCommand>().ToRabbitQueue(PinsMessaging.QueueNames.ImportCommands);
    }
}