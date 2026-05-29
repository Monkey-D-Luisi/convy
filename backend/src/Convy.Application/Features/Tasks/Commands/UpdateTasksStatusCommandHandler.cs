using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Tasks.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public class UpdateTasksStatusCommandHandler : IRequestHandler<UpdateTasksStatusCommand, Result<SmartTaskStatusBatchResult>>
{
    private readonly ITaskItemRepository _taskRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;

    public UpdateTasksStatusCommandHandler(
        ITaskItemRepository taskRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications,
        IActivityLogger activityLogger)
    {
        _taskRepository = taskRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _notifications = notifications;
        _activityLogger = activityLogger;
    }

    public async Task<Result<SmartTaskStatusBatchResult>> Handle(UpdateTasksStatusCommand request, CancellationToken cancellationToken)
    {
        var access = await TaskListAccess.GetAuthorizedTaskListAsync(
            request.ListId,
            _listRepository,
            _householdRepository,
            _currentUser,
            cancellationToken);

        if (access.IsFailure)
            return Result<SmartTaskStatusBatchResult>.Failure(access.Error!);

        var completed = new List<SmartMatchedTaskDto>();
        var uncompleted = new List<SmartMatchedTaskDto>();
        var unchanged = new List<SmartMatchedTaskDto>();
        var rejected = new List<SmartRejectedTaskInputDto>();
        var warnings = new List<SmartTaskWarningDto>();
        var changed = new List<TaskItem>();
        var seen = new HashSet<Guid>();

        foreach (var taskId in request.TaskIds)
        {
            if (!seen.Add(taskId))
            {
                rejected.Add(new SmartRejectedTaskInputDto(taskId.ToString(), "duplicate_in_request"));
                continue;
            }

            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task is null || task.ListId != request.ListId)
            {
                rejected.Add(new SmartRejectedTaskInputDto(taskId.ToString(), "not_found"));
                continue;
            }

            if (request.Status == SmartTaskStatus.Completed)
            {
                if (task.IsCompleted)
                {
                    unchanged.Add(new SmartMatchedTaskDto(task.Id, task.Title, "already_completed"));
                    continue;
                }

                task.Complete(_currentUser.UserId);
                changed.Add(task);
                completed.Add(new SmartMatchedTaskDto(task.Id, task.Title, "completed"));
            }
            else
            {
                if (!task.IsCompleted)
                {
                    unchanged.Add(new SmartMatchedTaskDto(task.Id, task.Title, "already_pending"));
                    continue;
                }

                task.Uncomplete();
                changed.Add(task);
                uncompleted.Add(new SmartMatchedTaskDto(task.Id, task.Title, "pending"));
            }
        }

        if (changed.Count > 0)
            await _taskRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        var userNames = new Dictionary<Guid, string> { [_currentUser.UserId] = user?.DisplayName ?? "Unknown" };
        foreach (var task in changed)
        {
            var dto = TaskItemMapper.ToDto(task, userNames);
            if (task.IsCompleted)
            {
                await _notifications.NotifyTaskCompleted(access.Value!.List.HouseholdId, dto, cancellationToken);
                await _activityLogger.LogAsync(access.Value.List.HouseholdId, ActivityEntityType.Task, task.Id, ActivityActionType.Completed, _currentUser.UserId, task.Title, cancellationToken);
            }
            else
            {
                await _notifications.NotifyTaskUncompleted(access.Value!.List.HouseholdId, dto, cancellationToken);
                await _activityLogger.LogAsync(access.Value.List.HouseholdId, ActivityEntityType.Task, task.Id, ActivityActionType.Uncompleted, _currentUser.UserId, task.Title, cancellationToken);
            }
        }

        return Result<SmartTaskStatusBatchResult>.Success(new SmartTaskStatusBatchResult(completed, uncompleted, unchanged, rejected, warnings));
    }
}
