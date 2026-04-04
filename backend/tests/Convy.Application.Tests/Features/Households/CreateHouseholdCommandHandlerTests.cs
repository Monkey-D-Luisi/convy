using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Households.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Households;

public class CreateHouseholdCommandHandlerTests
{
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly CreateHouseholdCommandHandler _handler;

    public CreateHouseholdCommandHandlerTests()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _handler = new CreateHouseholdCommandHandler(_householdRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateHouseholdCommand("My Home");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _householdRepository.Received(1).AddAsync(Arg.Any<Household>(), Arg.Any<CancellationToken>());
        await _householdRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
