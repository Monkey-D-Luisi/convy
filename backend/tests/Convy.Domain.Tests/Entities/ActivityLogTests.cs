using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class ActivityLogTests
{
    private readonly Guid _householdId = Guid.NewGuid();
    private readonly Guid _entityId = Guid.NewGuid();
    private readonly Guid _performedBy = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesActivityLog()
    {
        var before = DateTime.UtcNow;

        var log = new ActivityLog(
            _householdId,
            ActivityEntityType.Item,
            _entityId,
            ActivityActionType.Created,
            _performedBy);

        var after = DateTime.UtcNow;

        log.HouseholdId.Should().Be(_householdId);
        log.EntityType.Should().Be(ActivityEntityType.Item);
        log.EntityId.Should().Be(_entityId);
        log.ActionType.Should().Be(ActivityActionType.Created);
        log.PerformedBy.Should().Be(_performedBy);
        log.Metadata.Should().BeNull();
        log.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_WithMetadata_StoresMetadata()
    {
        var metadata = "{\"oldTitle\":\"Milk\",\"newTitle\":\"Oat Milk\"}";

        var log = new ActivityLog(
            _householdId,
            ActivityEntityType.Item,
            _entityId,
            ActivityActionType.Updated,
            _performedBy,
            metadata);

        log.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void Constructor_WithNullMetadata_MetadataIsNull()
    {
        var log = new ActivityLog(
            _householdId,
            ActivityEntityType.List,
            _entityId,
            ActivityActionType.Created,
            _performedBy,
            null);

        log.Metadata.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyHouseholdId_ThrowsArgumentException()
    {
        var act = () => new ActivityLog(
            Guid.Empty,
            ActivityEntityType.Item,
            _entityId,
            ActivityActionType.Created,
            _performedBy);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("householdId");
    }

    [Fact]
    public void Constructor_WithEmptyEntityId_ThrowsArgumentException()
    {
        var act = () => new ActivityLog(
            _householdId,
            ActivityEntityType.Item,
            Guid.Empty,
            ActivityActionType.Created,
            _performedBy);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("entityId");
    }

    [Fact]
    public void Constructor_WithEmptyPerformedBy_ThrowsArgumentException()
    {
        var act = () => new ActivityLog(
            _householdId,
            ActivityEntityType.Item,
            _entityId,
            ActivityActionType.Created,
            Guid.Empty);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("performedBy");
    }

    [Theory]
    [InlineData(ActivityEntityType.Item)]
    [InlineData(ActivityEntityType.List)]
    [InlineData(ActivityEntityType.Household)]
    [InlineData(ActivityEntityType.Invite)]
    public void Constructor_WithDifferentEntityTypes_SetsEntityType(ActivityEntityType entityType)
    {
        var log = new ActivityLog(
            _householdId,
            entityType,
            _entityId,
            ActivityActionType.Created,
            _performedBy);

        log.EntityType.Should().Be(entityType);
    }

    [Theory]
    [InlineData(ActivityActionType.Created)]
    [InlineData(ActivityActionType.Updated)]
    [InlineData(ActivityActionType.Completed)]
    [InlineData(ActivityActionType.Uncompleted)]
    [InlineData(ActivityActionType.Deleted)]
    [InlineData(ActivityActionType.Archived)]
    [InlineData(ActivityActionType.Renamed)]
    [InlineData(ActivityActionType.MemberJoined)]
    public void Constructor_WithDifferentActionTypes_SetsActionType(ActivityActionType actionType)
    {
        var log = new ActivityLog(
            _householdId,
            ActivityEntityType.Item,
            _entityId,
            actionType,
            _performedBy);

        log.ActionType.Should().Be(actionType);
    }
}
