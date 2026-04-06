using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Users.Queries;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Users;

public class GetUserProfileQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetUserProfileQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetUserProfileQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new GetUserProfileQueryHandler(_userRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsUserDto()
    {
        // Arrange
        var user = new User("firebase-uid", "Test User", "test@example.com");
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var query = new GetUserProfileQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.DisplayName.Should().Be("Test User");
        result.Value.Email.Should().Be("test@example.com");
        result.Value.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var query = new GetUserProfileQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }
}
