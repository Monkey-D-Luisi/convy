using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;

namespace Convy.Application.Features.Tasks;

internal static class TaskListAccess
{
    public static async Task<Result<(HouseholdList List, Household Household)>> GetAuthorizedTaskListAsync(
        Guid listId,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var list = await listRepository.GetByIdAsync(listId, cancellationToken);
        if (list is null)
            return Result<(HouseholdList, Household)>.Failure(Error.NotFound("List not found."));

        if (list.Type != ListType.Tasks)
            return Result<(HouseholdList, Household)>.Failure(Error.Validation("Tasks are only supported for task lists."));

        var household = await householdRepository.GetByIdWithMembersAsync(list.HouseholdId, cancellationToken);
        if (household is null || !household.IsMember(currentUser.UserId))
            return Result<(HouseholdList, Household)>.Failure(Error.Forbidden("You are not a member of this household."));

        return Result<(HouseholdList, Household)>.Success((list, household));
    }

    public static async Task<Result<(TaskItem Task, HouseholdList List, Household Household)>> GetAuthorizedTaskAsync(
        Guid taskId,
        ITaskItemRepository taskRepository,
        IHouseholdListRepository listRepository,
        IHouseholdRepository householdRepository,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null)
            return Result<(TaskItem, HouseholdList, Household)>.Failure(Error.NotFound("Task not found."));

        var access = await GetAuthorizedTaskListAsync(
            task.ListId,
            listRepository,
            householdRepository,
            currentUser,
            cancellationToken);

        return access.IsSuccess
            ? Result<(TaskItem, HouseholdList, Household)>.Success((task, access.Value!.List, access.Value.Household))
            : Result<(TaskItem, HouseholdList, Household)>.Failure(access.Error!);
    }
}
