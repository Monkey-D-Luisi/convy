using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Tasks.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public class SmartBatchCreateTasksCommandHandler : IRequestHandler<SmartBatchCreateTasksCommand, Result<SmartBatchCreateTasksResult>>
{
    private readonly ITaskItemRepository _taskRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdNotificationService _notifications;
    private readonly IActivityLogger _activityLogger;
    private readonly IUserFacingTextNormalizer _textNormalizer;

    public SmartBatchCreateTasksCommandHandler(
        ITaskItemRepository taskRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IHouseholdNotificationService notifications,
        IActivityLogger activityLogger,
        IUserFacingTextNormalizer textNormalizer)
    {
        _taskRepository = taskRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _notifications = notifications;
        _activityLogger = activityLogger;
        _textNormalizer = textNormalizer;
    }

    public async Task<Result<SmartBatchCreateTasksResult>> Handle(SmartBatchCreateTasksCommand request, CancellationToken cancellationToken)
    {
        var access = await TaskListAccess.GetAuthorizedTaskListAsync(
            request.ListId,
            _listRepository,
            _householdRepository,
            _currentUser,
            cancellationToken);

        if (access.IsFailure)
            return Result<SmartBatchCreateTasksResult>.Failure(access.Error!);

        var existingTasks = await _taskRepository.GetByListIdAsync(request.ListId, "All", null, null, null, cancellationToken);
        var existingByTitle = existingTasks
            .GroupBy(GetNormalizedTitle, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(task => task.IsCompleted).First(), StringComparer.Ordinal);

        var created = new List<SmartCreatedTaskDto>();
        var reused = new List<SmartMatchedTaskDto>();
        var uncompleted = new List<SmartMatchedTaskDto>();
        var unchanged = new List<SmartMatchedTaskDto>();
        var rejected = new List<SmartRejectedTaskInputDto>();
        var warnings = new List<SmartTaskWarningDto>();
        var seenInRequest = new Dictionary<string, SmartMatchedTaskDto>(StringComparer.Ordinal);
        var createdEntities = new List<TaskItem>();
        var uncompletedEntities = new List<TaskItem>();

        foreach (var input in request.Tasks)
        {
            var title = _textNormalizer.NormalizeTitle(input.Title);
            var normalizedTitle = _textNormalizer.NormalizeForComparison(title);
            if (normalizedTitle.Length == 0)
            {
                rejected.Add(new SmartRejectedTaskInputDto(input.Title, "empty_after_normalization"));
                continue;
            }

            if (seenInRequest.TryGetValue(normalizedTitle, out var duplicate))
            {
                reused.Add(duplicate with { Reason = "duplicate_in_request" });
                continue;
            }

            if (existingByTitle.TryGetValue(normalizedTitle, out var existing))
            {
                var match = new SmartMatchedTaskDto(existing.Id, existing.Title, existing.IsCompleted ? "was_completed" : "already_pending");
                seenInRequest[normalizedTitle] = match;

                if (!string.IsNullOrWhiteSpace(input.Note) && !string.Equals(existing.Note, input.Note.Trim(), StringComparison.Ordinal))
                {
                    reused.Add(match);
                    warnings.Add(new SmartTaskWarningDto(existing.Title, "note_conflict", $"{existing.Title} already exists with a different note. It was not changed."));
                    continue;
                }

                if (existing.IsCompleted)
                {
                    var tracked = await _taskRepository.GetByIdAsync(existing.Id, cancellationToken);
                    if (tracked is not null)
                    {
                        tracked.Uncomplete();
                        uncompletedEntities.Add(tracked);
                    }

                    uncompleted.Add(match);
                }
                else
                {
                    reused.Add(match);
                }

                continue;
            }

            var task = new TaskItem(title, normalizedTitle, request.ListId, _currentUser.UserId, input.Note);
            await _taskRepository.AddAsync(task, cancellationToken);
            createdEntities.Add(task);
            var createdDto = new SmartCreatedTaskDto(task.Id, task.Title, task.Note);
            created.Add(createdDto);
            seenInRequest[normalizedTitle] = new SmartMatchedTaskDto(task.Id, task.Title, "created_in_request");
        }

        if (createdEntities.Count > 0 || uncompletedEntities.Count > 0)
            await _taskRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
        var userNames = new Dictionary<Guid, string> { [_currentUser.UserId] = user?.DisplayName ?? "Unknown" };
        foreach (var task in createdEntities)
        {
            await _notifications.NotifyTaskCreated(access.Value!.List.HouseholdId, TaskItemMapper.ToDto(task, userNames), cancellationToken);
            await _activityLogger.LogAsync(access.Value.List.HouseholdId, ActivityEntityType.Task, task.Id, ActivityActionType.Created, _currentUser.UserId, task.Title, cancellationToken);
        }

        foreach (var task in uncompletedEntities)
        {
            await _notifications.NotifyTaskUncompleted(access.Value!.List.HouseholdId, TaskItemMapper.ToDto(task, userNames), cancellationToken);
            await _activityLogger.LogAsync(access.Value.List.HouseholdId, ActivityEntityType.Task, task.Id, ActivityActionType.Uncompleted, _currentUser.UserId, task.Title, cancellationToken);
        }

        return Result<SmartBatchCreateTasksResult>.Success(new SmartBatchCreateTasksResult(created, reused, uncompleted, unchanged, rejected, warnings));
    }

    private string GetNormalizedTitle(TaskItem task) =>
        string.IsNullOrWhiteSpace(task.NormalizedTitle)
            ? _textNormalizer.NormalizeForComparison(task.Title)
            : task.NormalizedTitle;
}
