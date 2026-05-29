using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public record SmartBatchCreateTasksCommand(
    Guid ListId,
    IReadOnlyList<SmartTaskInput> Tasks) : IRequest<Result<SmartBatchCreateTasksResult>>;

public record SmartTaskInput(string Title, string? Note);

public record SmartBatchCreateTasksResult(
    IReadOnlyList<SmartCreatedTaskDto> Created,
    IReadOnlyList<SmartMatchedTaskDto> Reused,
    IReadOnlyList<SmartMatchedTaskDto> Uncompleted,
    IReadOnlyList<SmartMatchedTaskDto> Unchanged,
    IReadOnlyList<SmartRejectedTaskInputDto> Rejected,
    IReadOnlyList<SmartTaskWarningDto> Warnings);

public record SmartCreatedTaskDto(Guid Id, string Title, string? Note);
public record SmartMatchedTaskDto(Guid Id, string Title, string Reason);
public record SmartRejectedTaskInputDto(string Title, string Reason);
public record SmartTaskWarningDto(string Title, string Reason, string Message);
