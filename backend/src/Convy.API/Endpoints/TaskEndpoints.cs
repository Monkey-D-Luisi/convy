using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Tasks.Commands;
using Convy.Application.Features.Tasks.Queries;
using Convy.API.Authorization;
using Convy.API.Services;
using Convy.Domain.ValueObjects;
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
            .RequireAuthorization("FirebaseOnly");

        group.MapPost("/parse-voice", async (
            Guid listId,
            HttpRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var audio = form.Files["audio"];
            var timeZoneId = form["timeZoneId"].ToString();
            if (audio is null)
                return Results.BadRequest(new Error("Validation", "Audio file is required."));
            if (string.IsNullOrWhiteSpace(timeZoneId))
                return Results.BadRequest(new Error("Validation", "Time zone ID is required."));
            if (!DateTimeOffset.TryParse(form["now"].ToString(), out var now))
                return Results.BadRequest(new Error("Validation", "Current date and time are required."));

            await using var stream = audio.OpenReadStream();
            var command = new ParseTaskVoiceAudioCommand(
                listId,
                stream,
                audio.FileName,
                timeZoneId,
                now,
                audio.Length);
            var result = await mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        })
        .DisableAntiforgery()
        .RequireAuthorization("FirebaseOnly");

        group.MapPut("/{taskId:guid}", async (
            Guid listId,
            Guid taskId,
            UpdateTaskRequest request,
            IMediator mediator) =>
        {
            var command = new UpdateTaskCommand(
                listId,
                taskId,
                request.Title,
                request.Note,
                request.AssignedToUserId,
                request.DueDate,
                request.ReminderAtUtc,
                request.Priority);
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
            .RequireAuthorization("FirebaseOnly");

        group.MapPost("/{taskId:guid}/uncomplete", UncompleteTaskWithMcpIdempotencyAsync)
            .RequireAuthorization("FirebaseOnly");

        group.MapPost("/batch", BatchCreateTasksWithFirebaseIdempotencyAsync)
            .RequireAuthorization("FirebaseOnly");

        group.MapPost("/smart-batch", SmartBatchCreateTasksWithMcpIdempotencyAsync)
            .RequireAuthorization(McpScopes.TasksWrite)
            .RequireRateLimiting("mcp-write");

        group.MapPost("/status-batch", UpdateTasksStatusWithMcpIdempotencyAsync)
            .RequireAuthorization(McpScopes.TasksWrite)
            .RequireRateLimiting("mcp-write");
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
            "firebase_create_task",
            new
            {
                listId,
                request.Title,
                request.Note,
                request.AssignedToUserId,
                request.DueDate,
                request.ReminderAtUtc,
                request.Priority,
            },
            async () =>
            {
                var command = new CreateTaskCommand(
                    listId,
                    request.Title,
                    request.Note,
                    request.AssignedToUserId,
                    request.DueDate,
                    request.ReminderAtUtc,
                    request.Priority);
                var result = await mediator.Send(command, cancellationToken);
                return result.IsSuccess
                    ? McpIdempotencySnapshot.Json(StatusCodes.Status201Created, $"/api/v1/lists/{listId}/tasks/{result.Value}", new { id = result.Value })
                    : ErrorToSnapshot(result.Error!);
            },
            cancellationToken);

        return snapshot.ToResult();
    }

    private static async Task<IResult> BatchCreateTasksWithFirebaseIdempotencyAsync(
        Guid listId,
        SmartBatchCreateTasksRequest request,
        IMediator mediator,
        McpWriteIdempotencyService idempotency,
        IUserFacingTextNormalizer textNormalizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var snapshot = await idempotency.ExecuteAsync(
            httpContext,
            "firebase_batch_create_tasks",
            BuildSmartBatchIdempotencyKey(listId, request, textNormalizer),
            async () =>
            {
                var result = await mediator.Send(BuildSmartBatchCommand(listId, request), cancellationToken);
                return result.IsSuccess
                    ? McpIdempotencySnapshot.Json(StatusCodes.Status200OK, null, result.Value!)
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
            "firebase_complete_task",
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
            "firebase_uncomplete_task",
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

    private static async Task<IResult> SmartBatchCreateTasksWithMcpIdempotencyAsync(
        Guid listId,
        SmartBatchCreateTasksRequest request,
        IMediator mediator,
        McpWriteIdempotencyService idempotency,
        IUserFacingTextNormalizer textNormalizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var snapshot = await idempotency.ExecuteAsync(
            httpContext,
            "convy_add_tasks",
            BuildSmartBatchIdempotencyKey(listId, request, textNormalizer),
            async () =>
            {
                var result = await mediator.Send(BuildSmartBatchCommand(listId, request), cancellationToken);
                return result.IsSuccess
                    ? McpIdempotencySnapshot.Json(StatusCodes.Status200OK, null, result.Value!)
                    : ErrorToSnapshot(result.Error!);
            },
            cancellationToken);

        return snapshot.ToResult();
    }

    private static SmartBatchCreateTasksCommand BuildSmartBatchCommand(Guid listId, SmartBatchCreateTasksRequest request) =>
        new(
            listId,
            request.Tasks.Select(task => new SmartTaskInput(
                task.Title,
                task.Note,
                task.AssignedToUserId,
                task.DueDate,
                task.ReminderAtUtc,
                task.Priority)).ToList());

    private static object BuildSmartBatchIdempotencyKey(
        Guid listId,
        SmartBatchCreateTasksRequest request,
        IUserFacingTextNormalizer textNormalizer) =>
        new
        {
            listId,
            tasks = request.Tasks.Select(task => new
            {
                title = textNormalizer.NormalizeForComparison(textNormalizer.NormalizeTitle(task.Title)),
                note = string.IsNullOrWhiteSpace(task.Note) ? null : task.Note.Trim(),
                task.AssignedToUserId,
                task.DueDate,
                task.ReminderAtUtc,
                task.Priority,
            }).ToArray()
        };

    private static async Task<IResult> UpdateTasksStatusWithMcpIdempotencyAsync(
        Guid listId,
        UpdateTasksStatusRequest request,
        IMediator mediator,
        McpWriteIdempotencyService idempotency,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SmartTaskStatus>(request.Status, ignoreCase: true, out var status))
            return Results.BadRequest(new { error = "invalid_status" });

        var snapshot = await idempotency.ExecuteAsync(
            httpContext,
            "convy_update_tasks_status",
            new { listId, taskIds = request.TaskIds, status },
            async () =>
            {
                var command = new UpdateTasksStatusCommand(listId, request.TaskIds, status);
                var result = await mediator.Send(command, cancellationToken);
                return result.IsSuccess
                    ? McpIdempotencySnapshot.Json(StatusCodes.Status200OK, null, result.Value!)
                    : ErrorToSnapshot(result.Error!);
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

public record CreateTaskRequest(
    string Title,
    string? Note,
    Guid? AssignedToUserId = null,
    DateOnly? DueDate = null,
    DateTime? ReminderAtUtc = null,
    TaskPriority Priority = TaskPriority.Normal);

public record UpdateTaskRequest(
    string Title,
    string? Note,
    Guid? AssignedToUserId = null,
    DateOnly? DueDate = null,
    DateTime? ReminderAtUtc = null,
    TaskPriority Priority = TaskPriority.Normal);
public record SmartBatchCreateTasksRequest(IReadOnlyList<SmartBatchCreateTaskRequest> Tasks);
public record SmartBatchCreateTaskRequest(
    string Title,
    string? Note,
    Guid? AssignedToUserId = null,
    DateOnly? DueDate = null,
    DateTime? ReminderAtUtc = null,
    TaskPriority Priority = TaskPriority.Normal);
public record UpdateTasksStatusRequest(IReadOnlyList<Guid> TaskIds, string Status);
