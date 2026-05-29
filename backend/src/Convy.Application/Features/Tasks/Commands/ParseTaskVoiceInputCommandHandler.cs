using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using MediatR;

namespace Convy.Application.Features.Tasks.Commands;

public class ParseTaskVoiceAudioCommandHandler : IRequestHandler<ParseTaskVoiceAudioCommand, Result<TaskVoiceParsingResult>>
{
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ITaskVoiceParsingService _voiceParsingService;
    private readonly ITaskItemRepository _taskRepository;
    private readonly IUserFacingTextNormalizer _textNormalizer;
    private readonly IUserRepository? _userRepository;

    public ParseTaskVoiceAudioCommandHandler(
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser,
        ITaskVoiceParsingService voiceParsingService,
        ITaskItemRepository taskRepository,
        IUserFacingTextNormalizer textNormalizer,
        IUserRepository? userRepository = null)
    {
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _currentUser = currentUser;
        _voiceParsingService = voiceParsingService;
        _taskRepository = taskRepository;
        _textNormalizer = textNormalizer;
        _userRepository = userRepository;
    }

    public async Task<Result<TaskVoiceParsingResult>> Handle(ParseTaskVoiceAudioCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken);
        if (list is null)
            return Result<TaskVoiceParsingResult>.Failure(Error.NotFound("List not found."));

        if (list.Type != ListType.Tasks)
            return Result<TaskVoiceParsingResult>.Failure(Error.Validation("Voice task parsing is only supported for task lists."));

        var household = await _householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(_currentUser.UserId))
            return Result<TaskVoiceParsingResult>.Failure(Error.Forbidden("You are not a member of this household."));

        var memberIds = household.Memberships.Select(m => m.UserId).Distinct().ToList();
        var userNames = _userRepository is null
            ? new Dictionary<Guid, string>()
            : (await _userRepository.GetByIdsAsync(memberIds, cancellationToken)).ToDictionary(u => u.Id, u => u.DisplayName);
        var members = memberIds
            .Select(id => new TaskVoiceHouseholdMember(id, userNames.GetValueOrDefault(id, id.ToString())))
            .ToList();

        var result = await _voiceParsingService.ParseAudioAsync(
            request.Audio,
            request.FileName,
            household.Id,
            members,
            request.TimeZoneId,
            request.Now,
            cancellationToken);

        var existingTasks = await _taskRepository.GetByListIdAsync(request.ListId, "All", null, null, null, cancellationToken);
        var existingByTitle = existingTasks
            .GroupBy(GetNormalizedTitle, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(task => task.IsCompleted).First(), StringComparer.Ordinal);
        var tasks = result.Tasks.Select(task =>
        {
            if (!string.IsNullOrWhiteSpace(task.MatchedExistingTask))
                return task;

            var normalizedTitle = _textNormalizer.NormalizeForComparison(_textNormalizer.NormalizeTitle(task.Title));
            return existingByTitle.TryGetValue(normalizedTitle, out var existing)
                ? task with { MatchedExistingTask = existing.Title }
                : task;
        }).ToList();

        result = result with { Tasks = tasks };

        return Result<TaskVoiceParsingResult>.Success(result);
    }

    private string GetNormalizedTitle(TaskItem task) =>
        string.IsNullOrWhiteSpace(task.NormalizedTitle)
            ? _textNormalizer.NormalizeForComparison(task.Title)
            : task.NormalizedTitle;
}
