using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Items;

public class ParseVoiceInputCommandHandlerTests
{
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ParseVoiceInputCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public ParseVoiceInputCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new ParseVoiceInputCommandHandler(
            _listRepository,
            _householdRepository,
            _currentUser);
    }

    private void SetupValidListAndHousehold(out Guid listId)
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        listId = list.Id;

        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>())
            .Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);
    }

    [Fact]
    public async Task Handle_WithValidText_ReturnsParsedItems()
    {
        // Arrange
        SetupValidListAndHousehold(out var listId);
        var command = new ParseVoiceInputCommand(listId, "milk");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Title.Should().Be("milk");
    }

    [Fact]
    public async Task Handle_WithCommaDelimitedText_SplitsCorrectly()
    {
        // Arrange
        SetupValidListAndHousehold(out var listId);
        var command = new ParseVoiceInputCommand(listId, "milk, bread, eggs");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value![0].Title.Should().Be("milk");
        result.Value[1].Title.Should().Be("bread");
        result.Value[2].Title.Should().Be("eggs");
    }

    [Fact]
    public async Task Handle_WithQuantityAndUnit_ExtractsCorrectly()
    {
        // Arrange
        SetupValidListAndHousehold(out var listId);
        var command = new ParseVoiceInputCommand(listId, "2 kg of chicken");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Quantity.Should().Be(2);
        result.Value[0].Unit.Should().Be("kg");
        result.Value[0].Title.Should().Be("chicken");
    }

    [Fact]
    public async Task Handle_WhenListNotFound_ReturnsNotFound()
    {
        // Arrange
        _listRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdList?)null);

        var command = new ParseVoiceInputCommand(Guid.NewGuid(), "milk");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

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

        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>())
            .Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var command = new ParseVoiceInputCommand(list.Id, "milk");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }

    [Fact]
    public async Task Handle_WithAndDelimiter_SplitsCorrectly()
    {
        // Arrange
        SetupValidListAndHousehold(out var listId);
        var command = new ParseVoiceInputCommand(listId, "milk and bread and eggs");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithQuantityNoUnit_ExtractsQuantity()
    {
        // Arrange
        SetupValidListAndHousehold(out var listId);
        var command = new ParseVoiceInputCommand(listId, "3 bottles of water");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Quantity.Should().Be(3);
        result.Value[0].Unit.Should().Be("bottles");
        result.Value[0].Title.Should().Be("water");
    }
}
