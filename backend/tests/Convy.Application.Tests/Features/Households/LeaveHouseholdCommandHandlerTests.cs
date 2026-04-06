using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Households.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Households;

public class LeaveHouseholdCommandHandlerTests
{
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHouseholdNotificationService _notifications = Substitute.For<IHouseholdNotificationService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly LeaveHouseholdCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public LeaveHouseholdCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new LeaveHouseholdCommandHandler(
            _householdRepository,
            _userRepository,
            _currentUser,
            _notifications,
            _activityLogger);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var household = new Household("Home", ownerId);
        household.AddMember(_userId);

        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var user = new User("firebase-uid", "Test User", "test@example.com");
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new LeaveHouseholdCommand(household.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        household.IsMember(_userId).Should().BeFalse();
        await _householdRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifications.Received(1).NotifyMemberLeft(household.Id, _userId, "Test User", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHouseholdNotFound_ReturnsNotFound()
    {
        // Arrange
        _householdRepository.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Household?)null);

        var command = new LeaveHouseholdCommand(Guid.NewGuid());

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

        var command = new LeaveHouseholdCommand(household.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }
}
