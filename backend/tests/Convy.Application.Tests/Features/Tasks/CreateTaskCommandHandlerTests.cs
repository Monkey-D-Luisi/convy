using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Tasks.Commands;
using Convy.Application.Features.Tasks.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Tasks;

public class CreateTaskCommandHandlerTests
{
    private readonly ITaskItemRepository _taskRepository = Substitute.For<ITaskItemRepository>();
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHouseholdNotificationService _notifications = Substitute.For<IHouseholdNotificationService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly CreateTaskCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public CreateTaskCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new User("firebase-uid", "Test User", "test@example.com"));
        _handler = new CreateTaskCommandHandler(
            _taskRepository,
            _listRepository,
            _householdRepository,
            _userRepository,
            _currentUser,
            _notifications,
            _activityLogger);
    }

    [Fact]
    public async Task Handle_WithTaskList_ReturnsSuccess()
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        var command = new CreateTaskCommand(list.Id, "Clean kitchen", "Before dinner");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _taskRepository.Received(1).AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        await _taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifications.Received(1).NotifyTaskCreated(household.Id, Arg.Any<TaskItemDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithShoppingList_ReturnsValidationFailure()
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        var command = new CreateTaskCommand(list.Id, "Clean kitchen", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
        await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }
}
