using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Result>
{
    private readonly ITaskItemRepository _taskRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;

    public DeleteTaskCommandHandler(
        ITaskItemRepository taskRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications,
        IActivityLogger activityLogger)
    {
        _taskRepository = taskRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
        _notifications = notifications;
        _activityLogger = activityLogger;
    }

    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var access = await TaskListAccess.GetAuthorizedTaskAsync(
            request.ListId,
            request.TaskId,
            _taskRepository,
            _listRepository,
            _householdRepository,
            _currentUser,
            cancellationToken);

        if (access.IsFailure)
            return Result.Failure(access.Error!);

        var task = access.Value!.Task;
        var taskId = task.Id;
        _taskRepository.Remove(task);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyTaskDeleted(access.Value.List.HouseholdId, taskId, cancellationToken);
        await _activityLogger.LogAsync(access.Value.List.HouseholdId, ActivityEntityType.Task, taskId, ActivityActionType.Deleted, _currentUser.UserId, cancellationToken: cancellationToken);

        return Result.Success();
    }
}
