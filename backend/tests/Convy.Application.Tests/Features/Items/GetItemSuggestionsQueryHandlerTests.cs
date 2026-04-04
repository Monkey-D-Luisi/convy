using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.DTOs;
using Convy.Application.Features.Items.Queries;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Items;

public class GetItemSuggestionsQueryHandlerTests
{
    private readonly IListItemRepository _itemRepository = Substitute.For<IListItemRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetItemSuggestionsQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetItemSuggestionsQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new GetItemSuggestionsQueryHandler(_itemRepository, _householdRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WithNoQuery_ReturnsSuggestions()
    {
        // Arrange
        var household = new Household("Home", _userId);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        _itemRepository.GetFrequentTitlesAsync(household.Id, null, 10, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "Milk", "Bread", "Eggs" });

        var query = new GetItemSuggestionsQuery(household.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Suggestions.Should().HaveCount(3);
        result.Value.Suggestions.Should().ContainInOrder("Milk", "Bread", "Eggs");
    }

    [Fact]
    public async Task Handle_WithSearchQuery_ReturnsFllterSuggestions()
    {
        // Arrange
        var household = new Household("Home", _userId);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        _itemRepository.GetFrequentTitlesAsync(household.Id, "mi", 10, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "Milk" });

        var query = new GetItemSuggestionsQuery(household.Id, "mi");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Suggestions.Should().HaveCount(1);
        result.Value.Suggestions[0].Should().Be("Milk");
    }

    [Fact]
    public async Task Handle_WhenNoSuggestions_ReturnsEmptyList()
    {
        // Arrange
        var household = new Household("Home", _userId);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        _itemRepository.GetFrequentTitlesAsync(household.Id, "xyz", 10, Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        var query = new GetItemSuggestionsQuery(household.Id, "xyz");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenHouseholdNotFound_ReturnsNotFound()
    {
        // Arrange
        _householdRepository.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Household?)null);

        var query = new GetItemSuggestionsQuery(Guid.NewGuid());

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
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);

        var query = new GetItemSuggestionsQuery(household.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }
}
