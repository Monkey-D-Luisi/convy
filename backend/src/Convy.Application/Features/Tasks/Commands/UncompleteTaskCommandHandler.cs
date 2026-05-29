using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Tasks.DTOs;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public class UncompleteTaskCommandHandler : IRequestHandler<UncompleteTaskCommand, Result>
{
    private readonly ITaskItemRepository _taskRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;

    public UncompleteTaskCommandHandler(
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

    public async Task<Result> Handle(UncompleteTaskCommand request, CancellationToken cancellationToken)
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
        task.Uncomplete();

        await _taskRepository.SaveChangesAsync(cancellationToken);

        var dto = await CreateDtoAsync(task, cancellationToken);
        await _notifications.NotifyTaskUncompleted(access.Value.List.HouseholdId, dto, cancellationToken);
        await _activityLogger.LogAsync(access.Value.List.HouseholdId, ActivityEntityType.Task, task.Id, ActivityActionType.Uncompleted, _currentUser.UserId, task.Title, cancellationToken);

        return Result.Success();
    }

    private async Task<TaskItemDto> CreateDtoAsync(Domain.Entities.TaskItem task, CancellationToken cancellationToken)
    {
        var userIds = new[] { task.CreatedBy }
            .Concat(task.AssignedToUserId.HasValue ? new[] { task.AssignedToUserId.Value } : Array.Empty<Guid>())
            .Distinct();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userNames = users.ToDictionary(u => u.Id, u => u.DisplayName);
        return TaskItemMapper.ToDto(task, userNames);
    }
}
