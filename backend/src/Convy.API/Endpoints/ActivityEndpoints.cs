using Convy.Application.Common.Models;
using Convy.Application.Features.Activity.Queries;
using MediatR;

namespace Convy.API.Endpoints;

public static class ActivityEndpoints
{
    public static void MapActivityEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/households/{householdId:guid}/activity")
            .WithTags("Activity")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid householdId,
            int? limit,
            DateTime? before,
            IMediator mediator) =>
        {
            var query = new GetHouseholdActivityQuery(householdId, limit ?? 50, before);
            var result = await mediator.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        });

        // Item history endpoint
        var itemGroup = routes.MapGroup("/api/v1/items/{itemId:guid}/history")
            .WithTags("Activity")
            .RequireAuthorization();

        itemGroup.MapGet("/", async (
            Guid itemId,
            IMediator mediator) =>
        {
            var query = new GetItemHistoryQuery(itemId);
            var result = await mediator.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        });
    }

    private static IResult MapError(Error error) => error.Code switch
    {
        "NotFound" => Results.NotFound(error),
        "Validation" => Results.BadRequest(error),
        "Forbidden" => Results.Forbid(),
        _ => Results.Problem(error.Message, statusCode: 500)
    };
}
