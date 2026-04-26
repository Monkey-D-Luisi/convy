using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Tasks.Commands;
using Convy.Application.Features.Tasks.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Tasks;

public class TaskCommandHandlerTests
{
    private readonly ITaskItemRepository _taskRepository = Substitute.For<ITaskItemRepository>();
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHouseholdNotificationService _notifications = Substitute.For<IHouseholdNotificationService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly Guid _userId = Guid.NewGuid();

    public TaskCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new User("firebase-uid", "Test User", "test@example.com"));
        _userRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { new("firebase-uid", "Test User", "test@example.com") });
    }

    [Fact]
    public async Task Update_WithTaskList_UpdatesTask()
    {
        var (household, list, task) = ArrangeTaskList();
        var handler = new UpdateTaskCommandHandler(_taskRepository, _listRepository, _householdRepository, _userRepository, _currentUser, _notifications, _activityLogger);

        var result = await handler.Handle(new UpdateTaskCommand(list.Id, task.Id, "Mop kitchen", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.Title.Should().Be("Mop kitchen");
        task.Note.Should().BeNull();
        await _taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifications.Received(1).NotifyTaskUpdated(household.Id, Arg.Any<TaskItemDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_WithTaskList_CompletesTask()
    {
        var (household, list, task) = ArrangeTaskList();
        var handler = new CompleteTaskCommandHandler(_taskRepository, _listRepository, _householdRepository, _userRepository, _currentUser, _notifications, _activityLogger);

        var result = await handler.Handle(new CompleteTaskCommand(list.Id, task.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.IsCompleted.Should().BeTrue();
        await _notifications.Received(1).NotifyTaskCompleted(household.Id, Arg.Any<TaskItemDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Uncomplete_WithTaskList_UncompletesTask()
    {
        var (household, list, task) = ArrangeTaskList();
        task.Complete(_userId);
        var handler = new UncompleteTaskCommandHandler(_taskRepository, _listRepository, _householdRepository, _userRepository, _currentUser, _notifications, _activityLogger);

        var result = await handler.Handle(new UncompleteTaskCommand(list.Id, task.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.IsCompleted.Should().BeFalse();
        await _notifications.Received(1).NotifyTaskUncompleted(household.Id, Arg.Any<TaskItemDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithTaskList_RemovesTask()
    {
        var (household, list, task) = ArrangeTaskList();
        var handler = new DeleteTaskCommandHandler(_taskRepository, _listRepository, _householdRepository, _currentUser, _notifications, _activityLogger);

        var result = await handler.Handle(new DeleteTaskCommand(list.Id, task.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _taskRepository.Received(1).Remove(task);
        await _notifications.Received(1).NotifyTaskDeleted(household.Id, task.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_WithShoppingList_ReturnsValidationFailure()
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        var task = new TaskItem("Clean kitchen", list.Id, _userId);
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var handler = new CompleteTaskCommandHandler(_taskRepository, _listRepository, _householdRepository, _userRepository, _currentUser, _notifications, _activityLogger);

        var result = await handler.Handle(new CompleteTaskCommand(list.Id, task.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
        await _taskRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_WhenTaskBelongsToDifferentList_ReturnsNotFound()
    {
        var (_, _, task) = ArrangeTaskList();
        var handler = new CompleteTaskCommandHandler(_taskRepository, _listRepository, _householdRepository, _userRepository, _currentUser, _notifications, _activityLogger);

        var result = await handler.Handle(new CompleteTaskCommand(Guid.NewGuid(), task.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
        task.IsCompleted.Should().BeFalse();
        await _taskRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifications.DidNotReceive().NotifyTaskCompleted(Arg.Any<Guid>(), Arg.Any<TaskItemDto>(), Arg.Any<CancellationToken>());
    }

    private (Household Household, HouseholdList List, TaskItem Task) ArrangeTaskList()
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, _userId);
        var task = new TaskItem("Clean kitchen", list.Id, _userId, "Before dinner");
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        return (household, list, task);
    }
}
