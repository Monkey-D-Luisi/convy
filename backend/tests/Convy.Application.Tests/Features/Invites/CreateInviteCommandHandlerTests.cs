using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Invites.Commands;
using Convy.Application.Features.Invites.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Invites;

public class CreateInviteCommandHandlerTests
{
    private readonly IInviteRepository _inviteRepository = Substitute.For<IInviteRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly CreateInviteCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public CreateInviteCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new CreateInviteCommandHandler(
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
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var command = new CreateInviteCommand(household.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.HouseholdId.Should().Be(household.Id);
        result.Value.Code.Should().NotBeNullOrWhiteSpace();
        result.Value.IsValid.Should().BeTrue();
        await _inviteRepository.Received(1).AddAsync(Arg.Any<Invite>(), Arg.Any<CancellationToken>());
        await _inviteRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _activityLogger.Received(1).LogAsync(
            household.Id,
            Arg.Any<Domain.ValueObjects.ActivityEntityType>(),
            Arg.Any<Guid>(),
            Arg.Any<Domain.ValueObjects.ActivityActionType>(),
            _userId,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHouseholdNotFound_ReturnsNotFound()
    {
        // Arrange
        _householdRepository.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Household?)null);

        var command = new CreateInviteCommand(Guid.NewGuid());

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

        var command = new CreateInviteCommand(household.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }
}
