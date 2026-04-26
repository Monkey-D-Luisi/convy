using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.Commands;
using Convy.Application.Features.Items.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Items;

public class CreateItemCommandHandlerTests
{
    private readonly IListItemRepository _itemRepository = Substitute.For<IListItemRepository>();
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHouseholdNotificationService _notifications = Substitute.For<IHouseholdNotificationService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly CreateItemCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public CreateItemCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        var user = new User("firebase-uid", "Test User", "test@example.com");
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _handler = new CreateItemCommandHandler(_itemRepository, _listRepository, _householdRepository, _userRepository, _currentUser, _notifications, _activityLogger);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);

        var command = new CreateItemCommand(list.Id, "Milk", 2, "liters", "Semi-skimmed", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _itemRepository.Received(1).AddAsync(Arg.Any<ListItem>(), Arg.Any<CancellationToken>());
        await _itemRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifications.Received(1).NotifyItemCreated(household.Id, Arg.Any<ListItemDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTitleOnly_ReturnsSuccess()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);

        var command = new CreateItemCommand(list.Id, "Bread", null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenListNotFound_ReturnsNotFound()
    {
        // Arrange
        _listRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdList?)null);

        var command = new CreateItemCommand(Guid.NewGuid(), "Milk", null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
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

        var command = new CreateItemCommand(list.Id, "Milk", null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }

    [Fact]
    public async Task Handle_WithTasksList_ReturnsValidationFailure()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);

        var command = new CreateItemCommand(list.Id, "Milk", null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
        await _itemRepository.DidNotReceive().AddAsync(Arg.Any<ListItem>(), Arg.Any<CancellationToken>());
    }
}
