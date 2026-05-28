using Convy.Application.Common.Models;
using Convy.Application.Features.Tasks.Commands;
using Convy.Application.Features.Tasks.Queries;
using Convy.API.Authorization;
using Convy.API.Services;
using MediatR;

namespace Convy.API.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/lists/{listId:guid}/tasks")
            .WithTags("Tasks")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid listId,
            string? status,
            Guid? createdBy,
            DateTime? fromDate,
            DateTime? toDate,
            IMediator mediator) =>
        {
            var query = new GetListTasksQuery(listId, status, createdBy, fromDate, toDate);
            var result = await mediator.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        })
        .RequireAuthorization(McpScopes.TasksRead);

        group.MapPost("/", CreateTaskWithMcpIdempotencyAsync)
            .RequireAuthorization(McpScopes.TasksWrite);

        group.MapPut("/{taskId:guid}", async (
            Guid listId,
            Guid taskId,
            UpdateTaskRequest request,
            IMediator mediator) =>
        {
            var command = new UpdateTaskCommand(listId, taskId, request.Title, request.Note);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error!);
        })
        .RequireAuthorization("FirebaseOnly");

        group.MapDelete("/{taskId:guid}", async (
            Guid listId,
            Guid taskId,
            IMediator mediator) =>
        {
            var command = new DeleteTaskCommand(listId, taskId);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error!);
        })
        .RequireAuthorization("FirebaseOnly");

        group.MapPost("/{taskId:guid}/complete", CompleteTaskWithMcpIdempotencyAsync)
            .RequireAuthorization(McpScopes.TasksWrite);

        group.MapPost("/{taskId:guid}/uncomplete", UncompleteTaskWithMcpIdempotencyAsync)
            .RequireAuthorization(McpScopes.TasksWrite);
    }

    private static IResult MapError(Error error) => error.Code switch
    {
        "NotFound" => Results.NotFound(error),
        "Validation" => Results.BadRequest(error),
        "Conflict" => Results.Conflict(error),
        "Forbidden" => Results.Forbid(),
        _ => Results.Problem(error.Message, statusCode: 500)
    };

    private static async Task<IResult> CreateTaskWithMcpIdempotencyAsync(
        Guid listId,
        CreateTaskRequest request,
        IMediator mediator,
        McpWriteIdempotencyService idempotency,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var snapshot = await idempotency.ExecuteAsync(
            httpContext,
            "convy_create_task",
            new { listId, request.Title, request.Note },
            async () =>
            {
                var command = new CreateTaskCommand(listId, request.Title, request.Note);
                var result = await mediator.Send(command, cancellationToken);
                return result.IsSuccess
                    ? McpIdempotencySnapshot.Json(StatusCodes.Status201Created, $"/api/v1/lists/{listId}/tasks/{result.Value}", new { id = result.Value })
                    : ErrorToSnapshot(result.Error!);
            },
            cancellationToken);

        return snapshot.ToResult();
    }

    private static async Task<IResult> CompleteTaskWithMcpIdempotencyAsync(
        Guid listId,
        Guid taskId,
        IMediator mediator,
        McpWriteIdempotencyService idempotency,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var snapshot = await idempotency.ExecuteAsync(
            httpContext,
            "convy_complete_task",
            new { listId, taskId },
            async () =>
            {
                var command = new CompleteTaskCommand(listId, taskId);
                var result = await mediator.Send(command, cancellationToken);
                return result.IsSuccess ? McpIdempotencySnapshot.NoContent() : ErrorToSnapshot(result.Error!);
            },
            cancellationToken);

        return snapshot.ToResult();
    }

    private static async Task<IResult> UncompleteTaskWithMcpIdempotencyAsync(
        Guid listId,
        Guid taskId,
        IMediator mediator,
        McpWriteIdempotencyService idempotency,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var snapshot = await idempotency.ExecuteAsync(
            httpContext,
            "convy_uncomplete_task",
            new { listId, taskId },
            async () =>
            {
                var command = new UncompleteTaskCommand(listId, taskId);
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

public record CreateTaskRequest(string Title, string? Note);
public record UpdateTaskRequest(string Title, string? Note);
