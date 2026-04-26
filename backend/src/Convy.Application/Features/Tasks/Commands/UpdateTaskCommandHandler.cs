using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Tasks.DTOs;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, Result>
{
    private readonly ITaskItemRepository _taskRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;

    public UpdateTaskCommandHandler(
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

    public async Task<Result> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
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
        task.Update(request.Title, request.Note);

        await _taskRepository.SaveChangesAsync(cancellationToken);

        var dto = await CreateDtoAsync(task, cancellationToken);
        await _notifications.NotifyTaskUpdated(access.Value.List.HouseholdId, dto, cancellationToken);
        await _activityLogger.LogAsync(access.Value.List.HouseholdId, ActivityEntityType.Task, task.Id, ActivityActionType.Updated, _currentUser.UserId, task.Title, cancellationToken);

        return Result.Success();
    }

    private async Task<TaskItemDto> CreateDtoAsync(Domain.Entities.TaskItem task, CancellationToken cancellationToken)
    {
        var userIds = new[] { task.CreatedBy }.Concat(
            task.CompletedBy.HasValue ? new[] { task.CompletedBy.Value } : Array.Empty<Guid>()).Distinct();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userNames = users.ToDictionary(u => u.Id, u => u.DisplayName);
        return TaskItemMapper.ToDto(task, userNames);
    }
}
