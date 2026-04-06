using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Invites.Queries;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Invites;

public class GetHouseholdInvitesQueryHandlerTests
{
    private readonly IInviteRepository _inviteRepository = Substitute.For<IInviteRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetHouseholdInvitesQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetHouseholdInvitesQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new GetHouseholdInvitesQueryHandler(
            _inviteRepository,
            _householdRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsInvites()
    {
        // Arrange
        var household = new Household("Home", _userId);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var invite1 = new Invite(household.Id, _userId, TimeSpan.FromDays(7));
        var invite2 = new Invite(household.Id, _userId, TimeSpan.FromDays(7));
        _inviteRepository.GetByHouseholdIdAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Invite> { invite1, invite2 });

        var query = new GetHouseholdInvitesQuery(household.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenHouseholdNotFound_ReturnsNotFound()
    {
        // Arrange
        _householdRepository.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Household?)null);

        var query = new GetHouseholdInvitesQuery(Guid.NewGuid());

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
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var query = new GetHouseholdInvitesQuery(household.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }

    [Fact]
    public async Task Handle_WhenNoInvites_ReturnsEmptyList()
    {
        // Arrange
        var household = new Household("Home", _userId);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        _inviteRepository.GetByHouseholdIdAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Invite>());

        var query = new GetHouseholdInvitesQuery(household.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
