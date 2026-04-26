using Convy.Application.Common.Models;
using Convy.Application.Features.Tasks.Commands;
using Convy.Application.Features.Tasks.Queries;
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
        });

        group.MapPost("/", async (
            Guid listId,
            CreateTaskRequest request,
            IMediator mediator) =>
        {
            var command = new CreateTaskCommand(listId, request.Title, request.Note);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/lists/{listId}/tasks/{result.Value}", new { id = result.Value })
                : MapError(result.Error!);
        });

        group.MapPut("/{taskId:guid}", async (
            Guid listId,
            Guid taskId,
            UpdateTaskRequest request,
            IMediator mediator) =>
        {
            var command = new UpdateTaskCommand(taskId, request.Title, request.Note);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error!);
        });

        group.MapDelete("/{taskId:guid}", async (
            Guid listId,
            Guid taskId,
            IMediator mediator) =>
        {
            var command = new DeleteTaskCommand(taskId);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error!);
        });

        group.MapPost("/{taskId:guid}/complete", async (
            Guid listId,
            Guid taskId,
            IMediator mediator) =>
        {
            var command = new CompleteTaskCommand(taskId);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : MapError(result.Error!);
        });

        group.MapPost("/{taskId:guid}/uncomplete", async (
            Guid listId,
            Guid taskId,
            IMediator mediator) =>
        {
            var command = new UncompleteTaskCommand(taskId);
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

public record CreateTaskRequest(string Title, string? Note);
public record UpdateTaskRequest(string Title, string? Note);
