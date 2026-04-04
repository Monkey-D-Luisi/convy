using Convy.Application.Common.Models;
using Convy.Application.Features.Items.Commands;
using Convy.Application.Features.Items.Queries;
using MediatR;

namespace Convy.API.Endpoints;

public static class ItemEndpoints
{
    public static void MapItemEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/lists/{listId:guid}/items")
            .WithTags("Items")
            .RequireAuthorization();

        group.MapPost("/", async (
            Guid listId,
            CreateItemRequest request,
            IMediator mediator) =>
        {
            var command = new CreateItemCommand(listId, request.Title, request.Quantity, request.Unit, request.Note);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/lists/{listId}/items/{result.Value}", new { id = result.Value })
                : MapError(result.Error!);
        });

        group.MapGet("/", async (
            Guid listId,
            bool? includeCompleted,
            IMediator mediator) =>
        {
            var query = new GetListItemsQuery(listId, includeCompleted ?? true);
            var result = await mediator.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        });

        group.MapPut("/{itemId:guid}", async (
            Guid listId,
            Guid itemId,
            UpdateItemRequest request,
            IMediator mediator) =>
        {
            var command = new UpdateItemCommand(itemId, request.Title, request.Quantity, request.Unit, request.Note);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error!);
        });

        group.MapDelete("/{itemId:guid}", async (
            Guid listId,
            Guid itemId,
            IMediator mediator) =>
        {
            var command = new DeleteItemCommand(itemId);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error!);
        });

        group.MapPost("/{itemId:guid}/complete", async (
            Guid listId,
            Guid itemId,
            IMediator mediator) =>
        {
            var command = new CompleteItemCommand(itemId);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error!);
        });

        group.MapPost("/{itemId:guid}/uncomplete", async (
            Guid listId,
            Guid itemId,
            IMediator mediator) =>
        {
            var command = new UncompleteItemCommand(itemId);
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

public record CreateItemRequest(string Title, int? Quantity, string? Unit, string? Note);
public record UpdateItemRequest(string Title, int? Quantity, string? Unit, string? Note);
