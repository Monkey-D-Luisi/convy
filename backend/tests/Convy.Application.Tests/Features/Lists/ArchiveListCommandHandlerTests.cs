using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Lists.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Lists;

public class ArchiveListCommandHandlerTests
{
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ArchiveListCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public ArchiveListCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new ArchiveListCommandHandler(_listRepository, _householdRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var householdId = Guid.NewGuid();
        var list = new HouseholdList("List", ListType.Shopping, householdId, _userId);
        var household = new Household("Home", _userId);

        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(householdId, Arg.Any<CancellationToken>()).Returns(household);

        var command = new ArchiveListCommand(list.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        list.IsArchived.Should().BeTrue();
        await _listRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenListNotFound_ReturnsNotFound()
    {
        // Arrange
        _listRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdList?)null);

        var command = new ArchiveListCommand(Guid.NewGuid());

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
        var householdId = Guid.NewGuid();
        var list = new HouseholdList("List", ListType.Shopping, householdId, Guid.NewGuid());
        var household = new Household("Home", Guid.NewGuid());

        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(householdId, Arg.Any<CancellationToken>()).Returns(household);

        var command = new ArchiveListCommand(list.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
    }

    [Fact]
    public async Task Handle_WhenAlreadyArchived_ReturnsFailure()
    {
        // Arrange
        var householdId = Guid.NewGuid();
        var list = new HouseholdList("List", ListType.Shopping, householdId, _userId);
        list.Archive(); // already archived
        var household = new Household("Home", _userId);

        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(householdId, Arg.Any<CancellationToken>()).Returns(household);

        var command = new ArchiveListCommand(list.Id);

        // Act & Assert — DomainException is thrown and not caught by handler
        var act = () => _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<Domain.Exceptions.DomainException>()
            .WithMessage("*already archived*");
    }
}
