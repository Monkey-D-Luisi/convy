using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.DTOs;
using Convy.Application.Features.Items.Queries;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Items;

public class CheckDuplicateItemQueryHandlerTests
{
    private readonly IListItemRepository _itemRepository = Substitute.For<IListItemRepository>();
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly CheckDuplicateItemQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public CheckDuplicateItemQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new CheckDuplicateItemQueryHandler(_itemRepository, _listRepository, _householdRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenDuplicatesExist_ReturnsHasPotentialDuplicates()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        var existingItem = new ListItem("Milk", list.Id, _userId, 2, "liters", null);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        _itemRepository.SearchByTitleInListAsync(list.Id, "Milk", Arg.Any<CancellationToken>())
            .Returns(new List<ListItem> { existingItem });

        var query = new CheckDuplicateItemQuery(list.Id, "Milk");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.HasPotentialDuplicates.Should().BeTrue();
        result.Value.PotentialDuplicates.Should().HaveCount(1);
        result.Value.PotentialDuplicates[0].Title.Should().Be("Milk");
        result.Value.PotentialDuplicates[0].Quantity.Should().Be(2);
        result.Value.PotentialDuplicates[0].Unit.Should().Be("liters");
    }

    [Fact]
    public async Task Handle_WhenNoDuplicates_ReturnsEmptyList()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        _itemRepository.SearchByTitleInListAsync(list.Id, "Bread", Arg.Any<CancellationToken>())
            .Returns(new List<ListItem>());

        var query = new CheckDuplicateItemQuery(list.Id, "Bread");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.HasPotentialDuplicates.Should().BeFalse();
        result.Value.PotentialDuplicates.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenListNotFound_ReturnsNotFound()
    {
        // Arrange
        _listRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdList?)null);

        var query = new CheckDuplicateItemQuery(Guid.NewGuid(), "Milk");

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

        var query = new CheckDuplicateItemQuery(list.Id, "Milk");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }
}
