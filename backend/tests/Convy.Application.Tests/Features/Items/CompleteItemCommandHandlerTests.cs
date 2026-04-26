using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.Commands;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Items;

public class CompleteItemCommandHandlerTests
{
    private readonly IListItemRepository _itemRepository = Substitute.For<IListItemRepository>();
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHouseholdNotificationService _notifications = Substitute.For<IHouseholdNotificationService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly CompleteItemCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public CompleteItemCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _userRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { new("firebase-uid", "Test User", "test@example.com") });
        _handler = new CompleteItemCommandHandler(_itemRepository, _listRepository, _householdRepository, _userRepository, _currentUser, _notifications, _activityLogger);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        var item = new ListItem("Milk", list.Id, _userId);
        _itemRepository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);

        var command = new CompleteItemCommand(list.Id, item.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.IsCompleted.Should().BeTrue();
        item.CompletedBy.Should().Be(_userId);
        await _itemRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifications.Received(1).NotifyItemCompleted(household.Id, Arg.Any<ListItemDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenItemNotFound_ReturnsNotFound()
    {
        // Arrange
        _itemRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ListItem?)null);

        var command = new CompleteItemCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
        await _notifications.DidNotReceive().NotifyItemCompleted(Arg.Any<Guid>(), Arg.Any<ListItemDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotMember_ReturnsForbidden()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var household = new Household("Home", otherUserId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, otherUserId);
        var item = new ListItem("Milk", list.Id, otherUserId);
        _itemRepository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);

        var command = new CompleteItemCommand(list.Id, item.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }

    [Fact]
    public async Task Handle_WhenItemBelongsToDifferentList_ReturnsNotFound()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        var item = new ListItem("Milk", list.Id, _userId);
        _itemRepository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var command = new CompleteItemCommand(Guid.NewGuid(), item.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
        item.IsCompleted.Should().BeFalse();
        await _itemRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifications.DidNotReceive().NotifyItemCompleted(Arg.Any<Guid>(), Arg.Any<ListItemDto>(), Arg.Any<CancellationToken>());
    }
}
