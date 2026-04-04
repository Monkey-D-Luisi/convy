using Convy.Domain.Entities;
using Convy.Domain.Exceptions;
using Convy.Domain.ValueObjects;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class HouseholdListTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesList()
    {
        // Arrange
        var householdId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        // Act
        var list = new HouseholdList("Weekly Shopping", ListType.Shopping, householdId, creatorId);

        // Assert
        list.Name.Should().Be("Weekly Shopping");
        list.Type.Should().Be(ListType.Shopping);
        list.HouseholdId.Should().Be(householdId);
        list.CreatedBy.Should().Be(creatorId);
        list.IsArchived.Should().BeFalse();
        list.ArchivedAt.Should().BeNull();
        list.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithTasksType_CreatesList()
    {
        // Arrange
        var householdId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        // Act
        var list = new HouseholdList("House Chores", ListType.Tasks, householdId, creatorId);

        // Assert
        list.Type.Should().Be(ListType.Tasks);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => new HouseholdList(name!, ListType.Shopping, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyHouseholdId_ThrowsArgumentException()
    {
        // Act
        var act = () => new HouseholdList("List", ListType.Shopping, Guid.Empty, Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyCreatorId_ThrowsArgumentException()
    {
        // Act
        var act = () => new HouseholdList("List", ListType.Shopping, Guid.NewGuid(), Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        // Arrange
        var list = new HouseholdList("Old Name", ListType.Shopping, Guid.NewGuid(), Guid.NewGuid());

        // Act
        list.Rename("New Name");

        // Assert
        list.Name.Should().Be("New Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var list = new HouseholdList("List", ListType.Shopping, Guid.NewGuid(), Guid.NewGuid());

        // Act
        var act = () => list.Rename(name!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Archive_WhenNotArchived_SetsArchivedState()
    {
        // Arrange
        var list = new HouseholdList("List", ListType.Shopping, Guid.NewGuid(), Guid.NewGuid());

        // Act
        list.Archive();

        // Assert
        list.IsArchived.Should().BeTrue();
        list.ArchivedAt.Should().NotBeNull();
        list.ArchivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ThrowsDomainException()
    {
        // Arrange
        var list = new HouseholdList("List", ListType.Shopping, Guid.NewGuid(), Guid.NewGuid());
        list.Archive();

        // Act
        var act = () => list.Archive();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already archived*");
    }
}
