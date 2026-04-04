using Convy.Application.Common.Models;
using Convy.Application.Features.Invites.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Convy.API.Endpoints;

public static class InviteEndpoints
{
    public static void MapInviteEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/invites")
            .WithTags("Invites")
            .RequireAuthorization();

        group.MapPost("/", async (
            CreateInviteRequest request,
            IMediator mediator) =>
        {
            var command = new CreateInviteCommand(request.HouseholdId);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/invites/{result.Value!.Id}", result.Value)
                : MapError(result.Error!);
        });

        group.MapPost("/join", async (
            JoinHouseholdRequest request,
            IMediator mediator) =>
        {
            var command = new JoinHouseholdCommand(request.InviteCode);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Ok(new { householdId = result.Value })
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

public record CreateInviteRequest(Guid HouseholdId);
public record JoinHouseholdRequest(string InviteCode);
