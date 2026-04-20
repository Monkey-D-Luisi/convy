using Convy.Application.Features.Users.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Users;

public class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _handler = new RegisterUserCommandHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_WithNewUser_CreatesAndReturnsUser()
    {
        // Arrange
        _userRepository.GetByFirebaseUidAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new RegisterUserCommand("firebase123", "John Doe", "john@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayName.Should().Be("John Doe");
        result.Value.Email.Should().Be("john@example.com");
        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingUser_ReturnsExistingWithoutCreating()
    {
        // Arrange
        var existingUser = new User("firebase123", "John Doe", "john@example.com");
        _userRepository.GetByFirebaseUidAsync("firebase123", Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var command = new RegisterUserCommand("firebase123", "John Doe", "john@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(existingUser.Id);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingEmailAndDifferentFirebaseUid_ReturnsConflictWithoutReassigningUid()
    {
        // Arrange
        var existingUser = new User("original-firebase-uid", "John Doe", "john@example.com");
        _userRepository.GetByFirebaseUidAsync("new-firebase-uid", Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userRepository.GetByEmailAsync("john@example.com", Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var command = new RegisterUserCommand("new-firebase-uid", "John Doe", "john@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
        existingUser.FirebaseUid.Should().Be("original-firebase-uid");
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
