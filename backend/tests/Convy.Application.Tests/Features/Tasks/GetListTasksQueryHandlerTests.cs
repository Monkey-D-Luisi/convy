using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Tasks.Queries;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Tasks;

public class GetListTasksQueryHandlerTests
{
    private readonly ITaskItemRepository _taskRepository = Substitute.For<ITaskItemRepository>();
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetListTasksQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetListTasksQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new GetListTasksQueryHandler(_taskRepository, _listRepository, _householdRepository, _userRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WithTaskList_ReturnsTasks()
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, _userId);
        var task = new TaskItem("Clean kitchen", list.Id, _userId, "Before dinner");
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        _taskRepository.GetByListIdAsync(list.Id, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });
        _userRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { new("firebase-uid", "Test User", "test@example.com") });

        var result = await _handler.Handle(new GetListTasksQuery(list.Id, null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].Title.Should().Be("Clean kitchen");
        result.Value[0].Note.Should().Be("Before dinner");
    }

    [Fact]
    public async Task Handle_WithShoppingList_ReturnsValidationFailure()
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);

        var result = await _handler.Handle(new GetListTasksQuery(list.Id, null, null, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }
}
