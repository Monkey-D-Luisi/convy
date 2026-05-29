using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Services;
using Convy.Application.Features.Items.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Items;

public class SmartBatchCreateItemsCommandHandlerTests
{
    private readonly IListItemRepository _itemRepository = Substitute.For<IListItemRepository>();
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IHouseholdNotificationService _notifications = Substitute.For<IHouseholdNotificationService>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly SmartBatchCreateItemsCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public SmartBatchCreateItemsCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new User("firebase-uid", "Test User", "test@example.com"));
        _handler = new SmartBatchCreateItemsCommandHandler(
            _itemRepository,
            _listRepository,
            _householdRepository,
            _userRepository,
            _currentUser,
            _notifications,
            _activityLogger,
            new UserFacingTextNormalizer());
    }

    [Fact]
    public async Task Handle_WithNewItems_CreatesNormalizedMcpItems()
    {
        var (household, list) = SetupShoppingList();
        var added = new List<ListItem>();
        _itemRepository.GetByListIdAsync(list.Id, "All", null, null, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ListItem>());
        _itemRepository.AddAsync(Arg.Do<ListItem>(added.Add), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var command = new SmartBatchCreateItemsCommand(list.Id, [
            new SmartShoppingItemInput(" leche ", null, null, null),
            new SmartShoppingItemInput("PAN", null, null, null),
            new SmartShoppingItemInput("huevos", 12, "unidades", null)
        ], ItemCreationSource.Mcp);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Select(item => item.Title).Should().Equal("Leche", "Pan", "Huevos");
        result.Value.Created.Should().OnlyContain(item => item.Source == ItemCreationSource.Mcp);
        result.Value.Reused.Should().BeEmpty();
        added.Select(item => item.NormalizedTitle).Should().Equal("leche", "pan", "huevos");
        await _notifications.Received(3).NotifyItemCreated(household.Id, Arg.Any<Convy.Application.Features.Items.DTOs.ListItemDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingPendingItem_ReusesWithoutDuplicate()
    {
        var (_, list) = SetupShoppingList();
        var existing = new ListItem("Leche", "leche", list.Id, _userId);
        _itemRepository.GetByListIdAsync(list.Id, "All", null, null, null, Arg.Any<CancellationToken>())
            .Returns([existing]);
        var command = new SmartBatchCreateItemsCommand(list.Id, [
            new SmartShoppingItemInput(" leche ", null, null, null)
        ], ItemCreationSource.Mcp);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().BeEmpty();
        result.Value.Reused.Should().ContainSingle(item => item.Id == existing.Id && item.Reason == "already_pending");
        await _itemRepository.DidNotReceive().AddAsync(Arg.Any<ListItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingCompletedItem_UncompletesWithoutDuplicate()
    {
        var (_, list) = SetupShoppingList();
        var existing = new ListItem("Leche", "leche", list.Id, _userId);
        existing.Complete(_userId);
        _itemRepository.GetByListIdAsync(list.Id, "All", null, null, null, Arg.Any<CancellationToken>())
            .Returns([existing]);
        _itemRepository.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        var command = new SmartBatchCreateItemsCommand(list.Id, [
            new SmartShoppingItemInput("leche", null, null, null)
        ], ItemCreationSource.Mcp);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.IsCompleted.Should().BeFalse();
        result.Value!.Uncompleted.Should().ContainSingle(item => item.Id == existing.Id && item.Reason == "was_completed");
        await _itemRepository.DidNotReceive().AddAsync(Arg.Any<ListItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithQuantityConflict_ReturnsWarningAndDoesNotCreateDuplicate()
    {
        var (_, list) = SetupShoppingList();
        var existing = new ListItem("Leche", "leche", list.Id, _userId, 1);
        _itemRepository.GetByListIdAsync(list.Id, "All", null, null, null, Arg.Any<CancellationToken>())
            .Returns([existing]);
        var command = new SmartBatchCreateItemsCommand(list.Id, [
            new SmartShoppingItemInput("leche", 2, "litros", null)
        ], ItemCreationSource.Mcp);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Warnings.Should().ContainSingle(warning => warning.Reason == "quantity_conflict");
        result.Value.Created.Should().BeEmpty();
        await _itemRepository.DidNotReceive().AddAsync(Arg.Any<ListItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateInSameRequest_CreatesOnlyOnce()
    {
        var (_, list) = SetupShoppingList();
        _itemRepository.GetByListIdAsync(list.Id, "All", null, null, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ListItem>());
        var command = new SmartBatchCreateItemsCommand(list.Id, [
            new SmartShoppingItemInput("leche", null, null, null),
            new SmartShoppingItemInput(" LECHE ", null, null, null)
        ], ItemCreationSource.Mcp);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().ContainSingle(item => item.Title == "Leche");
        result.Value.Reused.Should().ContainSingle(item => item.Reason == "duplicate_in_request");
        await _itemRepository.Received(1).AddAsync(Arg.Any<ListItem>(), Arg.Any<CancellationToken>());
    }

    private (Household Household, HouseholdList List) SetupShoppingList()
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        return (household, list);
    }
}
