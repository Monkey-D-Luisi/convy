using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Lists.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Lists;

public class RenameListCommandHandlerTests
{
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly RenameListCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public RenameListCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new RenameListCommandHandler(_listRepository, _householdRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var householdId = Guid.NewGuid();
        var list = new HouseholdList("Old Name", ListType.Shopping, householdId, _userId);
        var household = new Household("Home", _userId);

        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(householdId, Arg.Any<CancellationToken>()).Returns(household);

        var command = new RenameListCommand(list.Id, "New Name");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        list.Name.Should().Be("New Name");
        await _listRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenListNotFound_ReturnsNotFound()
    {
        // Arrange
        _listRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdList?)null);

        var command = new RenameListCommand(Guid.NewGuid(), "New Name");

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
        var householdId = Guid.NewGuid();
        var list = new HouseholdList("List", ListType.Shopping, householdId, Guid.NewGuid());
        var household = new Household("Home", Guid.NewGuid()); // different owner

        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(householdId, Arg.Any<CancellationToken>()).Returns(household);

        var command = new RenameListCommand(list.Id, "New Name");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }
}
