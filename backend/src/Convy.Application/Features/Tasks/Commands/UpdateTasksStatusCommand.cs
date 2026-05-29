using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public record UpdateTasksStatusCommand(
    Guid ListId,
    IReadOnlyList<Guid> TaskIds,
    SmartTaskStatus Status) : IRequest<Result<SmartTaskStatusBatchResult>>;

public enum SmartTaskStatus
{
    Pending = 0,
    Completed = 1,
}

public record SmartTaskStatusBatchResult(
    IReadOnlyList<SmartMatchedTaskDto> Completed,
    IReadOnlyList<SmartMatchedTaskDto> Uncompleted,
    IReadOnlyList<SmartMatchedTaskDto> Unchanged,
    IReadOnlyList<SmartRejectedTaskInputDto> Rejected,
    IReadOnlyList<SmartTaskWarningDto> Warnings);
