using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Invites.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Invites;

public class RevokeInviteCommandHandlerTests
{
    private readonly IInviteRepository _inviteRepository = Substitute.For<IInviteRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly RevokeInviteCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public RevokeInviteCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new RevokeInviteCommandHandler(
            _inviteRepository,
            _householdRepository,
            _currentUser,
            _activityLogger);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var household = new Household("Home", _userId);
        var invite = new Invite(household.Id, _userId, TimeSpan.FromDays(7));

        _inviteRepository.GetByIdAsync(invite.Id, Arg.Any<CancellationToken>())
            .Returns(invite);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var command = new RevokeInviteCommand(invite.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invite.RevokedAt.Should().NotBeNull();
        await _inviteRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenInviteNotFound_ReturnsNotFound()
    {
        // Arrange
        _inviteRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invite?)null);

        var command = new RevokeInviteCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task Handle_WhenHouseholdNotFound_ReturnsNotFound()
    {
        // Arrange
        var invite = new Invite(Guid.NewGuid(), _userId, TimeSpan.FromDays(7));
        _inviteRepository.GetByIdAsync(invite.Id, Arg.Any<CancellationToken>())
            .Returns(invite);
        _householdRepository.GetByIdWithMembersAsync(invite.HouseholdId, Arg.Any<CancellationToken>())
            .Returns((Household?)null);

        var command = new RevokeInviteCommand(invite.Id);

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
        var invite = new Invite(household.Id, otherUserId, TimeSpan.FromDays(7));

        _inviteRepository.GetByIdAsync(invite.Id, Arg.Any<CancellationToken>())
            .Returns(invite);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var command = new RevokeInviteCommand(invite.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }
}
