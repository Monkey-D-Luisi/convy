using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Lists.DTOs;
using Convy.Application.Features.Lists.Queries;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Lists;

public class GetHouseholdListsQueryHandlerTests
{
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetHouseholdListsQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetHouseholdListsQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new GetHouseholdListsQueryHandler(_listRepository, _householdRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsLists()
    {
        // Arrange
        var householdId = Guid.NewGuid();
        var household = new Household("Home", _userId);

        var lists = new List<HouseholdList>
        {
            new("Shopping", ListType.Shopping, householdId, _userId),
            new("Chores", ListType.Tasks, householdId, _userId)
        };

        _householdRepository.GetByIdWithMembersAsync(householdId, Arg.Any<CancellationToken>()).Returns(household);
        _listRepository.GetByHouseholdIdAsync(householdId, false, Arg.Any<CancellationToken>()).Returns(lists);

        var query = new GetHouseholdListsQuery(householdId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Name.Should().Be("Shopping");
        result.Value![1].Name.Should().Be("Chores");
    }

    [Fact]
    public async Task Handle_WhenHouseholdNotFound_ReturnsNotFound()
    {
        // Arrange
        _householdRepository.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Household?)null);

        var query = new GetHouseholdListsQuery(Guid.NewGuid());

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
        var householdId = Guid.NewGuid();
        var household = new Household("Home", Guid.NewGuid()); // different owner

        _householdRepository.GetByIdWithMembersAsync(householdId, Arg.Any<CancellationToken>()).Returns(household);

        var query = new GetHouseholdListsQuery(householdId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }
}
