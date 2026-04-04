using Convy.Domain.Entities;
using Convy.Domain.Exceptions;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class InviteTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInvite()
    {
        // Arrange
        var householdId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        // Act
        var invite = new Invite(householdId, creatorId, TimeSpan.FromDays(7));

        // Assert
        invite.HouseholdId.Should().Be(householdId);
        invite.CreatedBy.Should().Be(creatorId);
        invite.Code.Should().NotBeNullOrWhiteSpace();
        invite.Code.Should().HaveLength(8);
        invite.IsValid.Should().BeTrue();
        invite.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WithEmptyHouseholdId_ThrowsArgumentException()
    {
        var act = () => new Invite(Guid.Empty, Guid.NewGuid(), TimeSpan.FromDays(7));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyCreatorId_ThrowsArgumentException()
    {
        var act = () => new Invite(Guid.NewGuid(), Guid.Empty, TimeSpan.FromDays(7));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNegativeValidity_ThrowsArgumentException()
    {
        var act = () => new Invite(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromDays(-1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Use_WhenValid_MarksAsUsed()
    {
        // Arrange
        var invite = new Invite(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromDays(7));
        var userId = Guid.NewGuid();

        // Act
        invite.Use(userId);

        // Assert
        invite.UsedAt.Should().NotBeNull();
        invite.UsedBy.Should().Be(userId);
        invite.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Use_WhenAlreadyUsed_ThrowsDomainException()
    {
        // Arrange
        var invite = new Invite(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromDays(7));
        invite.Use(Guid.NewGuid());

        // Act
        var act = () => invite.Use(Guid.NewGuid());

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*no longer valid*");
    }

    [Fact]
    public void Revoke_WhenActive_MarksAsRevoked()
    {
        // Arrange
        var invite = new Invite(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromDays(7));

        // Act
        invite.Revoke();

        // Assert
        invite.RevokedAt.Should().NotBeNull();
        invite.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsDomainException()
    {
        // Arrange
        var invite = new Invite(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromDays(7));
        invite.Revoke();

        // Act
        var act = () => invite.Revoke();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already revoked*");
    }
}
