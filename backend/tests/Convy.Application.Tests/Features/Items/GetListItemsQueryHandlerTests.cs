using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.DTOs;
using Convy.Application.Features.Items.Queries;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Items;

public class GetListItemsQueryHandlerTests
{
    private readonly IListItemRepository _itemRepository = Substitute.For<IListItemRepository>();
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetListItemsQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetListItemsQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _userRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { new("firebase-uid", "Test User", "test@example.com") });
        _handler = new GetListItemsQueryHandler(_itemRepository, _listRepository, _householdRepository, _userRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsItemsWithUserNames()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        var items = new List<ListItem>
        {
            new("Milk", list.Id, _userId, 2, "liters", null),
            new("Bread", list.Id, _userId)
        };

        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        _itemRepository.GetByListIdAsync(list.Id, null, null, null, null, Arg.Any<CancellationToken>()).Returns(items);

        var query = new GetListItemsQuery(list.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Title.Should().Be("Milk");
        result.Value[1].Title.Should().Be("Bread");
        result.Value[0].CreatedByName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WhenListNotFound_ReturnsNotFound()
    {
        // Arrange
        _listRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdList?)null);

        var query = new GetListItemsQuery(Guid.NewGuid());

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
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);

        var query = new GetListItemsQuery(list.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }
}
