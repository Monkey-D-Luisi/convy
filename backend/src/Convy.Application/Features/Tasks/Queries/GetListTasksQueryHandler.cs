using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Tasks.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Tasks.Queries;

public class GetListTasksQueryHandler : IRequestHandler<GetListTasksQuery, Result<IReadOnlyList<TaskItemDto>>>
{
    private readonly ITaskItemRepository _taskRepository;
    private readonly IHouseholdListRepository _listRepository;
    private readonly IHouseholdRepository _householdRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetListTasksQueryHandler(
        ITaskItemRepository taskRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _taskRepository = taskRepository;
        _listRepository = listRepository;
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<TaskItemDto>>> Handle(GetListTasksQuery request, CancellationToken cancellationToken)
    {
        var access = await TaskListAccess.GetAuthorizedTaskListAsync(
            request.ListId,
            _listRepository,
            _householdRepository,
            _currentUser,
            cancellationToken);

        if (access.IsFailure)
            return Result<IReadOnlyList<TaskItemDto>>.Failure(access.Error!);

        var tasks = await _taskRepository.GetByListIdAsync(
            request.ListId,
            request.Status,
            request.CreatedBy,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        var userIds = tasks.Select(t => t.CreatedBy)
            .Concat(tasks.Where(t => t.CompletedBy.HasValue).Select(t => t.CompletedBy!.Value))
            .Distinct();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userNames = users.ToDictionary(u => u.Id, u => u.DisplayName);

        var dtos = tasks.Select(t => TaskItemMapper.ToDto(t, userNames)).ToList();

        return Result<IReadOnlyList<TaskItemDto>>.Success(dtos);
    }
}
