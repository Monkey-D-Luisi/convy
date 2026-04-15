using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.Commands;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Items;

public class BatchCreateItemsCommandHandlerTests
{
    private readonly IListItemRepository _itemRepository = Substitute.For<IListItemRepository>();
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHouseholdNotificationService _notifications = Substitute.For<IHouseholdNotificationService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly BatchCreateItemsCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public BatchCreateItemsCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        var user = new User("firebase-uid", "Test User", "test@example.com");
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _handler = new BatchCreateItemsCommandHandler(_itemRepository, _listRepository, _householdRepository, _userRepository, _currentUser, _notifications, _activityLogger);
    }

    [Fact]
    public async Task Handle_WithValidItems_ReturnsSuccessWithAllIds()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);

        var command = new BatchCreateItemsCommand(list.Id, new List<BatchItemDto>
        {
            new("Milk", 2, "liters", null),
            new("Bread", 1, null, "Whole wheat"),
            new("Eggs", 12, "units", null)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CreatedIds.Should().HaveCount(3);
        await _itemRepository.Received(3).AddAsync(Arg.Any<ListItem>(), Arg.Any<CancellationToken>());
        await _itemRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidItems_SendsNotificationForEachItem()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);

        var command = new BatchCreateItemsCommand(list.Id, new List<BatchItemDto>
        {
            new("Milk", null, null, null),
            new("Bread", null, null, null)
        });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _notifications.Received(2).NotifyItemCreated(household.Id, Arg.Any<ListItemDto>(), Arg.Any<CancellationToken>());
        await _activityLogger.Received(2).LogAsync(household.Id, ActivityEntityType.Item, Arg.Any<Guid>(), ActivityActionType.Created, _userId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSingleItem_ReturnsSuccess()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);

        var command = new BatchCreateItemsCommand(list.Id, new List<BatchItemDto>
        {
            new("Milk", 2, "liters", "Semi-skimmed")
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CreatedIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WhenListNotFound_ReturnsNotFound()
    {
        // Arrange
        _listRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdList?)null);

        var command = new BatchCreateItemsCommand(Guid.NewGuid(), new List<BatchItemDto>
        {
            new("Milk", null, null, null)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
        await _itemRepository.DidNotReceive().AddAsync(Arg.Any<ListItem>(), Arg.Any<CancellationToken>());
        await _notifications.DidNotReceive().NotifyItemCreated(Arg.Any<Guid>(), Arg.Any<ListItemDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotMember_ReturnsForbidden()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var household = new Household("Home", otherUserId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, otherUserId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);

        var command = new BatchCreateItemsCommand(list.Id, new List<BatchItemDto>
        {
            new("Milk", null, null, null)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
        await _itemRepository.DidNotReceive().AddAsync(Arg.Any<ListItem>(), Arg.Any<CancellationToken>());
    }
}
