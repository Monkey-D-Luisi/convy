using Convy.Domain.Entities;
using Convy.Domain.Exceptions;
using Convy.Domain.ValueObjects;
using FluentAssertions;

namespace Convy.Domain.Tests.Entities;

public class ListItemTests
{
    private readonly Guid _listId = Guid.NewGuid();
    private readonly Guid _creatorId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithTitleOnly_CreatesItem()
    {
        var item = new ListItem("Milk", _listId, _creatorId);

        item.Title.Should().Be("Milk");
        item.ListId.Should().Be(_listId);
        item.CreatedBy.Should().Be(_creatorId);
        item.Quantity.Should().BeNull();
        item.Unit.Should().BeNull();
        item.Note.Should().BeNull();
        item.IsCompleted.Should().BeFalse();
        item.CompletedBy.Should().BeNull();
        item.CompletedAt.Should().BeNull();
        item.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithAllFields_CreatesItem()
    {
        var item = new ListItem("Milk", _listId, _creatorId, 2, "liters", "Semi-skimmed");

        item.Title.Should().Be("Milk");
        item.Quantity.Should().Be(2);
        item.Unit.Should().Be("liters");
        item.Note.Should().Be("Semi-skimmed");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ThrowsArgumentException(string? title)
    {
        var act = () => new ListItem(title!, _listId, _creatorId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyListId_ThrowsArgumentException()
    {
        var act = () => new ListItem("Milk", Guid.Empty, _creatorId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyCreatorId_ThrowsArgumentException()
    {
        var act = () => new ListItem("Milk", _listId, Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidQuantity_ThrowsArgumentException(int quantity)
    {
        var act = () => new ListItem("Milk", _listId, _creatorId, quantity);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithValidData_UpdatesFields()
    {
        var item = new ListItem("Milk", _listId, _creatorId, 2, "liters", "Old note");

        item.Update("Bread", 1, "loaf", "Whole wheat");

        item.Title.Should().Be("Bread");
        item.Quantity.Should().Be(1);
        item.Unit.Should().Be("loaf");
        item.Note.Should().Be("Whole wheat");
    }

    [Fact]
    public void Update_WithNullOptionalFields_ClearsFields()
    {
        var item = new ListItem("Milk", _listId, _creatorId, 2, "liters", "Note");

        item.Update("Milk", null, null, null);

        item.Quantity.Should().BeNull();
        item.Unit.Should().BeNull();
        item.Note.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidTitle_ThrowsArgumentException(string? title)
    {
        var item = new ListItem("Milk", _listId, _creatorId);

        var act = () => item.Update(title!, null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithInvalidQuantity_ThrowsArgumentException()
    {
        var item = new ListItem("Milk", _listId, _creatorId);

        var act = () => item.Update("Milk", 0, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Complete_WithValidUser_MarksCompleted()
    {
        var item = new ListItem("Milk", _listId, _creatorId);
        var completerId = Guid.NewGuid();

        item.Complete(completerId);

        item.IsCompleted.Should().BeTrue();
        item.CompletedBy.Should().Be(completerId);
        item.CompletedAt.Should().NotBeNull();
        item.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Complete_WithEmptyUserId_ThrowsArgumentException()
    {
        var item = new ListItem("Milk", _listId, _creatorId);

        var act = () => item.Complete(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ThrowsDomainException()
    {
        var item = new ListItem("Milk", _listId, _creatorId);
        item.Complete(Guid.NewGuid());

        var act = () => item.Complete(Guid.NewGuid());

        act.Should().Throw<DomainException>()
            .WithMessage("Item is already completed.");
    }

    [Fact]
    public void Uncomplete_WhenCompleted_ResetsCompletion()
    {
        var item = new ListItem("Milk", _listId, _creatorId);
        item.Complete(Guid.NewGuid());

        item.Uncomplete();

        item.IsCompleted.Should().BeFalse();
        item.CompletedBy.Should().BeNull();
        item.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Uncomplete_WhenNotCompleted_ThrowsDomainException()
    {
        var item = new ListItem("Milk", _listId, _creatorId);

        var act = () => item.Uncomplete();

        act.Should().Throw<DomainException>()
            .WithMessage("Item is not completed.");
    }

    [Fact]
    public void SetRecurrence_WithValidData_SetsProperties()
    {
        var item = new ListItem("Milk", _listId, _creatorId);

        item.SetRecurrence(RecurrenceFrequency.Weekly, 2);

        item.RecurrenceFrequency.Should().Be(RecurrenceFrequency.Weekly);
        item.RecurrenceInterval.Should().Be(2);
        item.NextDueDate.Should().NotBeNull();
        item.NextDueDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(14), TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetRecurrence_WithZeroOrNegativeInterval_ThrowsArgumentException(int interval)
    {
        var item = new ListItem("Milk", _listId, _creatorId);

        var act = () => item.SetRecurrence(RecurrenceFrequency.Daily, interval);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ClearRecurrence_WhenSet_ClearsProperties()
    {
        var item = new ListItem("Milk", _listId, _creatorId);
        item.SetRecurrence(RecurrenceFrequency.Daily, 1);

        item.ClearRecurrence();

        item.RecurrenceFrequency.Should().BeNull();
        item.RecurrenceInterval.Should().BeNull();
        item.NextDueDate.Should().BeNull();
    }

    [Fact]
    public void AdvanceRecurrence_WhenNoRecurrence_ThrowsDomainException()
    {
        var item = new ListItem("Milk", _listId, _creatorId);

        var act = () => item.AdvanceRecurrence();

        act.Should().Throw<DomainException>()
            .WithMessage("Item does not have a recurrence rule.");
    }

    [Fact]
    public void AdvanceRecurrence_WhenSet_UpdatesNextDueDate()
    {
        var item = new ListItem("Milk", _listId, _creatorId);
        item.SetRecurrence(RecurrenceFrequency.Daily, 3);
        var previousDueDate = item.NextDueDate;

        item.AdvanceRecurrence();

        item.NextDueDate.Should().NotBeNull();
        item.NextDueDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(3), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SetRecurrence_WithMonthlyFrequency_CalculatesCorrectDueDate()
    {
        var item = new ListItem("Milk", _listId, _creatorId);

        item.SetRecurrence(RecurrenceFrequency.Monthly, 1);

        item.NextDueDate.Should().BeCloseTo(DateTime.UtcNow.AddMonths(1), TimeSpan.FromSeconds(5));
    }
}
