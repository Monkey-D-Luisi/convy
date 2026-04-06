using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Activity.Queries;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Activity;

public class GetItemHistoryQueryHandlerTests
{
    private readonly IActivityLogRepository _activityRepository = Substitute.For<IActivityLogRepository>();
    private readonly IListItemRepository _itemRepository = Substitute.For<IListItemRepository>();
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetItemHistoryQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetItemHistoryQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new GetItemHistoryQueryHandler(
            _activityRepository,
            _itemRepository,
            _listRepository,
            _householdRepository,
            _userRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WithValidItem_ReturnsHistory()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        var item = new ListItem("Milk", list.Id, _userId);

        _itemRepository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(item);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>())
            .Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var log = new ActivityLog(household.Id, ActivityEntityType.Item, item.Id, ActivityActionType.Created, _userId);
        _activityRepository.GetByEntityIdAsync(item.Id, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ActivityLog> { log }.AsReadOnly());

        var user = new User("firebase-uid", "Test User", "test@example.com");
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetItemHistoryQuery(item.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].EntityId.Should().Be(item.Id);
        result.Value[0].PerformedByName.Should().Be("Test User");
    }

    [Fact]
    public async Task Handle_WhenItemNotFound_ReturnsNotFound()
    {
        // Arrange
        _itemRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ListItem?)null);

        var query = new GetItemHistoryQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task Handle_WhenNotMember_ReturnsForbidden()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var household = new Household("Home", otherUserId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, otherUserId);
        var item = new ListItem("Milk", list.Id, otherUserId);

        _itemRepository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(item);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>())
            .Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var query = new GetItemHistoryQuery(item.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }

    [Fact]
    public async Task Handle_WhenListNotFound_ReturnsNotFound()
    {
        // Arrange
        var item = new ListItem("Milk", Guid.NewGuid(), _userId);
        _itemRepository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(item);
        _listRepository.GetByIdAsync(item.ListId, Arg.Any<CancellationToken>())
            .Returns((HouseholdList?)null);

        var query = new GetItemHistoryQuery(item.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }
}
