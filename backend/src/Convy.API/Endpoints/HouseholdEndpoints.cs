using Convy.Application.Common.Models;
using Convy.Application.Features.Households.Commands;
using Convy.Application.Features.Households.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Convy.API.Endpoints;

public static class HouseholdEndpoints
{
    public static void MapHouseholdEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/households")
            .WithTags("Households")
            .RequireAuthorization();

        group.MapPost("/", async (
            CreateHouseholdRequest request,
            IMediator mediator) =>
        {
            var command = new CreateHouseholdCommand(request.Name);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/households/{result.Value}", new { id = result.Value })
                : MapError(result.Error!);
        });

        group.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetMyHouseholdsQuery());

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetHouseholdQuery(id));

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        });
    }

    private static IResult MapError(Error error) => error.Code switch
    {
        "NotFound" => Results.NotFound(error),
        "Validation" => Results.BadRequest(error),
        "Conflict" => Results.Conflict(error),
        "Forbidden" => Results.Forbid(),
        _ => Results.Problem(error.Message, statusCode: 500)
    };
}

public record CreateHouseholdRequest(string Name);
