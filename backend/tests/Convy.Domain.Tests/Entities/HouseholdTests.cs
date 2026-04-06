using Convy.Domain.Entities;
using Convy.Domain.Exceptions;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class HouseholdTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesHousehold()
    {
        // Arrange
        var creatorId = Guid.NewGuid();

        // Act
        var household = new Household("Test Home", creatorId);

        // Assert
        household.Name.Should().Be("Test Home");
        household.CreatedBy.Should().Be(creatorId);
        household.Memberships.Should().HaveCount(1);
        household.Memberships.First().UserId.Should().Be(creatorId);
        household.Memberships.First().Role.Should().Be(ValueObjects.HouseholdRole.Owner);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => new Household(name!, Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyCreatorId_ThrowsArgumentException()
    {
        // Act
        var act = () => new Household("Home", Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddMember_WithNewUser_AddsMembership()
    {
        // Arrange
        var household = new Household("Home", Guid.NewGuid());
        var newUserId = Guid.NewGuid();

        // Act
        var membership = household.AddMember(newUserId);

        // Assert
        household.Memberships.Should().HaveCount(2);
        membership.UserId.Should().Be(newUserId);
        membership.Role.Should().Be(ValueObjects.HouseholdRole.Member);
    }

    [Fact]
    public void AddMember_WhenAlreadyMember_ThrowsDomainException()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var household = new Household("Home", creatorId);

        // Act
        var act = () => household.AddMember(creatorId);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already a member*");
    }

    [Fact]
    public void IsMember_WithExistingMember_ReturnsTrue()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var household = new Household("Home", creatorId);

        // Act & Assert
        household.IsMember(creatorId).Should().BeTrue();
    }

    [Fact]
    public void IsMember_WithNonMember_ReturnsFalse()
    {
        // Arrange
        var household = new Household("Home", Guid.NewGuid());

        // Act & Assert
        household.IsMember(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        // Arrange
        var household = new Household("Old Name", Guid.NewGuid());

        // Act
        household.Rename("New Name");

        // Assert
        household.Name.Should().Be("New Name");
    }

    [Fact]
    public void Rename_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var household = new Household("Home", Guid.NewGuid());

        // Act
        var act = () => household.Rename("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveMember_WithExistingMember_RemovesMembership()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var household = new Household("Home", creatorId);
        var memberId = Guid.NewGuid();
        household.AddMember(memberId);

        // Act
        household.RemoveMember(memberId);

        // Assert
        household.Memberships.Should().HaveCount(1);
        household.IsMember(memberId).Should().BeFalse();
    }

    [Fact]
    public void RemoveMember_WithEmptyId_ThrowsArgumentException()
    {
        // Arrange
        var household = new Household("Home", Guid.NewGuid());

        // Act
        var act = () => household.RemoveMember(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveMember_WhenNotMember_ThrowsDomainException()
    {
        // Arrange
        var household = new Household("Home", Guid.NewGuid());

        // Act
        var act = () => household.RemoveMember(Guid.NewGuid());

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*not a member*");
    }

    [Fact]
    public void RemoveMember_WhenSoleOwner_ThrowsDomainException()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var household = new Household("Home", creatorId);

        // Act
        var act = () => household.RemoveMember(creatorId);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*sole owner*");
    }
}
