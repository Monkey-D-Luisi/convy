using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Households.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Households;

public class RenameHouseholdCommandHandlerTests
{
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHouseholdNotificationService _notifications = Substitute.For<IHouseholdNotificationService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly RenameHouseholdCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public RenameHouseholdCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new RenameHouseholdCommandHandler(
            _householdRepository,
            _currentUser,
            _notifications,
            _activityLogger);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var household = new Household("Old Name", _userId);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var command = new RenameHouseholdCommand(household.Id, "New Name");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        household.Name.Should().Be("New Name");
        await _householdRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifications.Received(1).NotifyHouseholdRenamed(household.Id, "New Name", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHouseholdNotFound_ReturnsNotFound()
    {
        // Arrange
        _householdRepository.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Household?)null);

        var command = new RenameHouseholdCommand(Guid.NewGuid(), "New Name");

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
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var command = new RenameHouseholdCommand(household.Id, "New Name");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }
}
