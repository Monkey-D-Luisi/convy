using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Activity.DTOs;
using Convy.Application.Features.Activity.Queries;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Activity;

public class GetHouseholdActivityQueryHandlerTests
{
    private readonly IActivityLogRepository _activityRepository = Substitute.For<IActivityLogRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetHouseholdActivityQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetHouseholdActivityQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new GetHouseholdActivityQueryHandler(
            _activityRepository,
            _householdRepository,
            _userRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsActivityLogs()
    {
        // Arrange
        var household = new Household("Home", _userId);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var log1 = new ActivityLog(household.Id, ActivityEntityType.Item, Guid.NewGuid(), ActivityActionType.Created, _userId);
        var log2 = new ActivityLog(household.Id, ActivityEntityType.List, Guid.NewGuid(), ActivityActionType.Updated, _userId);
        _activityRepository.GetByHouseholdIdAsync(household.Id, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ActivityLog> { log1, log2 }.AsReadOnly());

        var user = new User("firebase-uid", "Test User", "test@example.com");
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        var query = new GetHouseholdActivityQuery(household.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].HouseholdId.Should().Be(household.Id);
        result.Value[0].PerformedByName.Should().Be("Test User");
        result.Value[1].PerformedByName.Should().Be("Test User");
    }

    [Fact]
    public async Task Handle_WhenHouseholdNotFound_ReturnsNotFound()
    {
        // Arrange
        _householdRepository.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Household?)null);

        var query = new GetHouseholdActivityQuery(Guid.NewGuid());

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

        var query = new GetHouseholdActivityQuery(household.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }

    [Fact]
    public async Task Handle_WhenNoLogs_ReturnsEmptyList()
    {
        // Arrange
        var household = new Household("Home", _userId);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        _activityRepository.GetByHouseholdIdAsync(household.Id, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ActivityLog>().AsReadOnly());

        var query = new GetHouseholdActivityQuery(household.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsUnknownDisplayName()
    {
        // Arrange
        var household = new Household("Home", _userId);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var unknownPerformerId = Guid.NewGuid();
        var log = new ActivityLog(household.Id, ActivityEntityType.Item, Guid.NewGuid(), ActivityActionType.Created, unknownPerformerId);
        _activityRepository.GetByHouseholdIdAsync(household.Id, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ActivityLog> { log }.AsReadOnly());

        _userRepository.GetByIdAsync(unknownPerformerId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var query = new GetHouseholdActivityQuery(household.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].PerformedByName.Should().Be("Unknown");
    }
}
