using Convy.Application.Common.Models;
using Convy.Application.Features.Items.Commands;
using Convy.Application.Features.Items.Queries;
using Convy.Domain.ValueObjects;
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
            var command = new CreateItemCommand(listId, request.Title, request.Quantity, request.Unit, request.Note, request.RecurrenceFrequency, request.RecurrenceInterval);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/lists/{listId}/items/{result.Value}", new { id = result.Value })
                : MapError(result.Error!);
        });

        group.MapGet("/", async (
            Guid listId,
            string? status,
            Guid? createdBy,
            DateTime? fromDate,
            DateTime? toDate,
            IMediator mediator) =>
        {
            var query = new GetListItemsQuery(listId, status, createdBy, fromDate, toDate);
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
            var command = new UpdateItemCommand(itemId, request.Title, request.Quantity, request.Unit, request.Note, request.RecurrenceFrequency, request.RecurrenceInterval);
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

        group.MapGet("/check-duplicate", async (
            Guid listId,
            string title,
            IMediator mediator) =>
        {
            var query = new CheckDuplicateItemQuery(listId, title);
            var result = await mediator.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        });

        // Suggestions endpoint scoped to household
        var suggestionsGroup = routes.MapGroup("/api/v1/households/{householdId:guid}/item-suggestions")
            .WithTags("Items")
            .RequireAuthorization();

        suggestionsGroup.MapGet("/", async (
            Guid householdId,
            string? query,
            IMediator mediator) =>
        {
            var suggestionsQuery = new GetItemSuggestionsQuery(householdId, query);
            var result = await mediator.Send(suggestionsQuery);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        });

        group.MapPost("/batch", async (
            Guid listId,
            BatchCreateItemsRequest request,
            IMediator mediator) =>
        {
            var command = new BatchCreateItemsCommand(listId,
                request.Items.Select(i => new Application.Features.Items.Commands.BatchItemDto(i.Title, i.Quantity, i.Unit, i.Note)).ToList());
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/lists/{listId}/items", new { createdIds = result.Value!.CreatedIds })
                : MapError(result.Error!);
        });

        group.MapPost("/parse-voice", async (
            Guid listId,
            IFormFile audio,
            IMediator mediator) =>
        {
            await using var stream = audio.OpenReadStream();
            var command = new ParseVoiceAudioCommand(listId, stream, audio.FileName);
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        })
        .DisableAntiforgery();
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

public record CreateItemRequest(string Title, int? Quantity, string? Unit, string? Note, RecurrenceFrequency? RecurrenceFrequency, int? RecurrenceInterval);
public record UpdateItemRequest(string Title, int? Quantity, string? Unit, string? Note, RecurrenceFrequency? RecurrenceFrequency, int? RecurrenceInterval);
public record BatchCreateItemsRequest(List<BatchCreateItemRequest> Items);
public record BatchCreateItemRequest(string Title, int? Quantity, string? Unit, string? Note);
