using Convy.Application.Common.Models;
using Convy.Application.Features.Items.Commands;
using Convy.Application.Features.Items.Queries;
using Convy.API.Authorization;
using Convy.API.Services;
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

        group.MapPost("/", CreateItemWithMcpIdempotencyAsync)
            .RequireAuthorization(McpScopes.ItemsWrite);

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
        })
        .RequireAuthorization(McpScopes.ItemsRead);

        group.MapPut("/{itemId:guid}", async (
            Guid listId,
            Guid itemId,
            UpdateItemRequest request,
            IMediator mediator) =>
        {
            var command = new UpdateItemCommand(listId, itemId, request.Title, request.Quantity, request.Unit, request.Note, request.RecurrenceFrequency, request.RecurrenceInterval);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error!);
        })
        .RequireAuthorization("FirebaseOnly");

        group.MapDelete("/{itemId:guid}", async (
            Guid listId,
            Guid itemId,
            IMediator mediator) =>
        {
            var command = new DeleteItemCommand(listId, itemId);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error!);
        })
        .RequireAuthorization("FirebaseOnly");

        group.MapPost("/{itemId:guid}/complete", CompleteItemWithMcpIdempotencyAsync)
            .RequireAuthorization(McpScopes.ItemsWrite);

        group.MapPost("/{itemId:guid}/uncomplete", UncompleteItemWithMcpIdempotencyAsync)
            .RequireAuthorization(McpScopes.ItemsWrite);

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
        })
        .RequireAuthorization(McpScopes.ItemsRead);

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
        })
        .RequireAuthorization(McpScopes.ItemsRead);

        group.MapPost("/batch", async (
            Guid listId,
            BatchCreateItemsRequest request,
            IMediator mediator) =>
        {
            // Membership authorization belongs in the handler so every caller uses the same boundary.
            var source = Enum.TryParse<ItemCreationSource>(request.Source ?? nameof(ItemCreationSource.Manual), ignoreCase: true, out var parsedSource)
                ? parsedSource
                : ItemCreationSource.Manual;
            var command = new BatchCreateItemsCommand(listId,
                request.Items.Select(i => new Application.Features.Items.Commands.BatchItemDto(i.Title, i.Quantity, i.Unit, i.Note)).ToList(),
                source);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/lists/{listId}/items", new { createdIds = result.Value!.CreatedIds })
                : MapError(result.Error!);
        })
        .RequireAuthorization("FirebaseOnly");

        group.MapPost("/parse-voice", async (
            Guid listId,
            IFormFile audio,
            IMediator mediator) =>
        {
            await using var stream = audio.OpenReadStream();
            var command = new ParseVoiceAudioCommand(listId, stream, audio.FileName, audio.Length);
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Ok(new VoiceParseResponse(result.Value!.Transcription, result.Value.Items))
                : MapError(result.Error!);
        })
        .RequireAuthorization("FirebaseOnly")
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

    private static async Task<IResult> CreateItemWithMcpIdempotencyAsync(
        Guid listId,
        CreateItemRequest request,
        IMediator mediator,
        McpWriteIdempotencyService idempotency,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var snapshot = await idempotency.ExecuteAsync(
            httpContext,
            "convy_create_shopping_item",
            new
            {
                listId,
                request.Title,
                request.Quantity,
                request.Unit,
                request.Note,
                request.RecurrenceFrequency,
                request.RecurrenceInterval,
            },
            async () =>
            {
                var command = new CreateItemCommand(listId, request.Title, request.Quantity, request.Unit, request.Note, request.RecurrenceFrequency, request.RecurrenceInterval);
                var result = await mediator.Send(command, cancellationToken);
                return result.IsSuccess
                    ? McpIdempotencySnapshot.Json(StatusCodes.Status201Created, $"/api/v1/lists/{listId}/items/{result.Value}", new { id = result.Value })
                    : ErrorToSnapshot(result.Error!);
            },
            cancellationToken);

        return snapshot.ToResult();
    }

    private static async Task<IResult> CompleteItemWithMcpIdempotencyAsync(
        Guid listId,
        Guid itemId,
        IMediator mediator,
        McpWriteIdempotencyService idempotency,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var snapshot = await idempotency.ExecuteAsync(
            httpContext,
            "convy_complete_shopping_item",
            new { listId, itemId },
            async () =>
            {
                var command = new CompleteItemCommand(listId, itemId);
                var result = await mediator.Send(command, cancellationToken);
                return result.IsSuccess ? McpIdempotencySnapshot.NoContent() : ErrorToSnapshot(result.Error!);
            },
            cancellationToken);

        return snapshot.ToResult();
    }

    private static async Task<IResult> UncompleteItemWithMcpIdempotencyAsync(
        Guid listId,
        Guid itemId,
        IMediator mediator,
        McpWriteIdempotencyService idempotency,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var snapshot = await idempotency.ExecuteAsync(
            httpContext,
            "convy_uncomplete_shopping_item",
            new { listId, itemId },
            async () =>
            {
                var command = new UncompleteItemCommand(listId, itemId);
                var result = await mediator.Send(command, cancellationToken);
                return result.IsSuccess ? McpIdempotencySnapshot.NoContent() : ErrorToSnapshot(result.Error!);
            },
            cancellationToken);

        return snapshot.ToResult();
    }

    private static McpIdempotencySnapshot ErrorToSnapshot(Error error) => error.Code switch
    {
        "NotFound" => McpIdempotencySnapshot.Json(StatusCodes.Status404NotFound, null, error),
        "Validation" => McpIdempotencySnapshot.Json(StatusCodes.Status400BadRequest, null, error),
        "Conflict" => McpIdempotencySnapshot.Json(StatusCodes.Status409Conflict, null, error),
        "Forbidden" => McpIdempotencySnapshot.Empty(StatusCodes.Status403Forbidden),
        _ => McpIdempotencySnapshot.Json(StatusCodes.Status500InternalServerError, null, new { message = error.Message })
    };
}

public record CreateItemRequest(string Title, int? Quantity, string? Unit, string? Note, RecurrenceFrequency? RecurrenceFrequency, int? RecurrenceInterval);
public record UpdateItemRequest(string Title, int? Quantity, string? Unit, string? Note, RecurrenceFrequency? RecurrenceFrequency, int? RecurrenceInterval);
public record BatchCreateItemsRequest(List<BatchCreateItemRequest> Items, string? Source = null);
public record BatchCreateItemRequest(string Title, int? Quantity, string? Unit, string? Note);
public record VoiceParseResponse(string Transcription, List<ParsedItemDto> Items);
