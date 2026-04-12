using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Households.Queries;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Households;

public class GetHouseholdQueryHandlerTests
{
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetHouseholdQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public GetHouseholdQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new GetHouseholdQueryHandler(_householdRepository, _userRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WithExistingHousehold_ReturnsHouseholdDetail()
    {
        // Arrange
        var user = new User("firebase_uid", "Test User", "test@test.com");
        var household = new Household("Test", user.Id);
        _currentUser.UserId.Returns(user.Id);

        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);
        _userRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user }.AsReadOnly());

        var query = new GetHouseholdQuery(household.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test");
        result.Value.Members.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithNonExistentHousehold_ReturnsNotFound()
    {
        // Arrange
        _householdRepository.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Household?)null);

        var query = new GetHouseholdQuery(Guid.NewGuid());

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
        var household = new Household("Test", otherUserId);

        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var query = new GetHouseholdQuery(household.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }
}
