using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Services;
using Convy.Application.Features.Tasks.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Tasks;

public class SmartBatchCreateTasksCommandHandlerTests
{
    private readonly ITaskItemRepository _taskRepository = Substitute.For<ITaskItemRepository>();
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHouseholdNotificationService _notifications = Substitute.For<IHouseholdNotificationService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly SmartBatchCreateTasksCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public SmartBatchCreateTasksCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new User("firebase-uid", "Test User", "test@example.com"));
        _handler = new SmartBatchCreateTasksCommandHandler(
            _taskRepository,
            _listRepository,
            _householdRepository,
            _userRepository,
            _currentUser,
            _notifications,
            _activityLogger,
            new UserFacingTextNormalizer());
    }

    [Fact]
    public async Task Handle_WithNewTasks_CreatesNormalizedTasks()
    {
        var (_, list) = SetupTaskList();
        var added = new List<TaskItem>();
        _taskRepository.GetByListIdAsync(list.Id, "All", null, null, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _taskRepository.AddAsync(Arg.Do<TaskItem>(added.Add), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var command = new SmartBatchCreateTasksCommand(list.Id, [
            new SmartTaskInput(" limpiar cocina ", null),
            new SmartTaskInput("SACAR BASURA", "noche")
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Select(task => task.Title).Should().Equal("Limpiar cocina", "Sacar basura");
        added.Select(task => task.NormalizedTitle).Should().Equal("limpiar cocina", "sacar basura");
    }

    [Fact]
    public async Task Handle_WithStructuredFields_CreatesTaskWithMetadata()
    {
        var (household, list) = SetupTaskList();
        var assignee = Guid.NewGuid();
        household.AddMember(assignee);
        var added = new List<TaskItem>();
        _taskRepository.GetByListIdAsync(list.Id, "All", null, null, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _taskRepository.AddAsync(Arg.Do<TaskItem>(added.Add), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var reminderAtUtc = new DateTime(2026, 5, 30, 7, 0, 0, DateTimeKind.Utc);
        var command = new SmartBatchCreateTasksCommand(list.Id, [
            new SmartTaskInput(
                "limpiar cocina",
                "antes de comer",
                assignee,
                new DateOnly(2026, 5, 30),
                reminderAtUtc,
                TaskPriority.High)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added.Should().ContainSingle();
        added[0].AssignedToUserId.Should().Be(assignee);
        added[0].DueDate.Should().Be(new DateOnly(2026, 5, 30));
        added[0].ReminderAtUtc.Should().Be(reminderAtUtc);
        added[0].Priority.Should().Be(TaskPriority.High);
        result.Value!.Created.Should().ContainSingle(task => task.AssignedToUserId == assignee);
    }

    [Fact]
    public async Task Handle_WithCompletedMatch_UncompletesTask()
    {
        var (_, list) = SetupTaskList();
        var existing = new TaskItem("Limpiar cocina", "limpiar cocina", list.Id, _userId);
        existing.Complete(_userId);
        _taskRepository.GetByListIdAsync(list.Id, "All", null, null, null, Arg.Any<CancellationToken>())
            .Returns([existing]);
        _taskRepository.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        var command = new SmartBatchCreateTasksCommand(list.Id, [
            new SmartTaskInput("limpiar   cocina", null)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.IsCompleted.Should().BeFalse();
        result.Value!.Uncompleted.Should().ContainSingle(task => task.Id == existing.Id && task.Reason == "was_completed");
        await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    private (Household Household, HouseholdList List) SetupTaskList()
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        return (household, list);
    }
}
