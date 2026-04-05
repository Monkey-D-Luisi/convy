using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Lists.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Lists;

public class CreateListCommandHandlerTests
{
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHouseholdNotificationService _notifications = Substitute.For<IHouseholdNotificationService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly CreateListCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public CreateListCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new CreateListCommandHandler(_listRepository, _householdRepository, _currentUser, _notifications, _activityLogger);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var householdId = Guid.NewGuid();
        var household = new Household("Home", _userId);
        _householdRepository.GetByIdWithMembersAsync(householdId, Arg.Any<CancellationToken>())
            .Returns(household);

        var command = new CreateListCommand(householdId, "Weekly Shopping", ListType.Shopping);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _listRepository.Received(1).AddAsync(Arg.Any<HouseholdList>(), Arg.Any<CancellationToken>());
        await _listRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHouseholdNotFound_ReturnsNotFound()
    {
        // Arrange
        _householdRepository.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Household?)null);

        var command = new CreateListCommand(Guid.NewGuid(), "List", ListType.Shopping);

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
        var household = new Household("Home", Guid.NewGuid()); // different creator
        _householdRepository.GetByIdWithMembersAsync(householdId, Arg.Any<CancellationToken>())
            .Returns(household);

        var command = new CreateListCommand(householdId, "List", ListType.Shopping);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }
}
