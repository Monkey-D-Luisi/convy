using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Common.Services;
using Convy.Application.Features.Tasks.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<Guid>>
{
    private readonly ITaskItemRepository _taskRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;
    private readonly IUserFacingTextNormalizer _textNormalizer;

    public CreateTaskCommandHandler(
        ITaskItemRepository taskRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications,
        IActivityLogger activityLogger,
        IUserFacingTextNormalizer? textNormalizer = null)
    {
        _taskRepository = taskRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _notifications = notifications;
        _activityLogger = activityLogger;
        _textNormalizer = textNormalizer ?? new UserFacingTextNormalizer();
    }

    public async Task<Result<Guid>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var access = await TaskListAccess.GetAuthorizedTaskListAsync(
            request.ListId,
            _listRepository,
            _householdRepository,
            _currentUser,
            cancellationToken);

        if (access.IsFailure)
            return Result<Guid>.Failure(access.Error!);

        if (request.AssignedToUserId.HasValue && !access.Value!.Household.IsMember(request.AssignedToUserId.Value))
            return Result<Guid>.Failure(Error.Validation("Assigned user must be a member of the household."));

        var title = _textNormalizer.NormalizeTitle(request.Title);
        var normalizedTitle = _textNormalizer.NormalizeForComparison(title);
        var task = new TaskItem(
            title,
            normalizedTitle,
            request.ListId,
            _currentUser.UserId,
            request.Note,
            request.AssignedToUserId,
            request.DueDate,
            request.ReminderAtUtc,
            request.Priority);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var dto = await CreateDtoAsync(task, cancellationToken);

        await _notifications.NotifyTaskCreated(access.Value.List.HouseholdId, dto, cancellationToken);
        await _activityLogger.LogAsync(access.Value.List.HouseholdId, ActivityEntityType.Task, task.Id, ActivityActionType.Created, _currentUser.UserId, task.Title, cancellationToken);

        return Result<Guid>.Success(task.Id);
    }

    private async Task<TaskItemDto> CreateDtoAsync(TaskItem task, CancellationToken cancellationToken)
    {
        var userIds = new[] { task.CreatedBy }
            .Concat(task.AssignedToUserId.HasValue ? new[] { task.AssignedToUserId.Value } : Array.Empty<Guid>())
            .Distinct();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userNames = users.ToDictionary(u => u.Id, u => u.DisplayName);
        return TaskItemMapper.ToDto(task, userNames);
    }
}
