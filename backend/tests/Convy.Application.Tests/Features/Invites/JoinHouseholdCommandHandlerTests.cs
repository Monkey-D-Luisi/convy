using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Invites.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Convy.Application.Tests.Features.Invites;

public class JoinHouseholdCommandHandlerTests
{
    private readonly IInviteRepository _inviteRepository = Substitute.For<IInviteRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHouseholdNotificationService _notifications = Substitute.For<IHouseholdNotificationService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly ILogger<JoinHouseholdCommandHandler> _logger = Substitute.For<ILogger<JoinHouseholdCommandHandler>>();
    private readonly JoinHouseholdCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public JoinHouseholdCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new User("firebase-uid", "Test User", "test@example.com"));
        _handler = new JoinHouseholdCommandHandler(_inviteRepository, _householdRepository, _userRepository, _currentUser, _notifications, _activityLogger, _logger);
    }

    [Fact]
    public async Task Handle_WithValidInvite_JoinsHousehold()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var household = new Household("Test Home", ownerId);
        var invite = new Invite(household.Id, ownerId, TimeSpan.FromDays(7));

        _inviteRepository.GetByCodeAsync(invite.Code, Arg.Any<CancellationToken>())
            .Returns(invite);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var command = new JoinHouseholdCommand(invite.Code);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(household.Id);
        household.Memberships.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNonExistentInvite_ReturnsNotFound()
    {
        // Arrange
        _inviteRepository.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Invite?)null);

        var command = new JoinHouseholdCommand("INVALID");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task Handle_WhenAlreadyMember_ReturnsConflict()
    {
        // Arrange
        var household = new Household("Test Home", _userId);
        var invite = new Invite(household.Id, _userId, TimeSpan.FromDays(7));

        _inviteRepository.GetByCodeAsync(invite.Code, Arg.Any<CancellationToken>())
            .Returns(invite);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var command = new JoinHouseholdCommand(invite.Code);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }
}
