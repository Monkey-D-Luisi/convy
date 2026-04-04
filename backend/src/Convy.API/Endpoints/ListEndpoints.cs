using Convy.Application.Common.Models;
using Convy.Application.Features.Lists.Commands;
using Convy.Application.Features.Lists.Queries;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.API.Endpoints;

public static class ListEndpoints
{
    public static void MapListEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/households/{householdId:guid}/lists")
            .WithTags("Lists")
            .RequireAuthorization();

        group.MapPost("/", async (
            Guid householdId,
            CreateListRequest request,
            IMediator mediator) =>
        {
            var command = new CreateListCommand(householdId, request.Name, request.Type);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/households/{householdId}/lists/{result.Value}", new { id = result.Value })
                : MapError(result.Error!);
        });

        group.MapGet("/", async (
            Guid householdId,
            bool? includeArchived,
            IMediator mediator) =>
        {
            var query = new GetHouseholdListsQuery(householdId, includeArchived ?? false);
            var result = await mediator.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        });

        group.MapPut("/{listId:guid}/name", async (
            Guid householdId,
            Guid listId,
            RenameListRequest request,
            IMediator mediator) =>
        {
            var command = new RenameListCommand(listId, request.NewName);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error!);
        });

        group.MapPost("/{listId:guid}/archive", async (
            Guid householdId,
            Guid listId,
            IMediator mediator) =>
        {
            var command = new ArchiveListCommand(listId);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
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

public record CreateListRequest(string Name, ListType Type);
public record RenameListRequest(string NewName);
