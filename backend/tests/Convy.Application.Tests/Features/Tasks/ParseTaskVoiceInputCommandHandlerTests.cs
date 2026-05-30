using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Services;
using Convy.Application.Features.Tasks.Commands;
using Convy.Domain.Common;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Tasks;

public class ParseTaskVoiceInputCommandHandlerTests
{
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ITaskVoiceParsingService _voiceParsing = Substitute.For<ITaskVoiceParsingService>();
    private readonly ITaskItemRepository _taskRepository = Substitute.For<ITaskItemRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserFacingTextNormalizer _textNormalizer = new UserFacingTextNormalizer();
    private readonly Guid _userId = Guid.NewGuid();

    public ParseTaskVoiceInputCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _taskRepository.GetByListIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<Guid?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Fact]
    public async Task Handle_WithTaskList_ReturnsParsedTasks()
    {
        var household = new Household("Home", _userId);
        var assignee = Guid.NewGuid();
        household.AddMember(assignee);
        _userRepository.GetByIdsAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(assignee)), Arg.Any<CancellationToken>())
            .Returns([CreateUser(assignee, "Marina")]);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        var expected = new TaskVoiceParsingResult(
            "recuerdame limpiar la cocina mañana",
            [
                new ParsedTaskDto(
                    "Limpiar la cocina",
                    null,
                    assignee,
                    new DateOnly(2026, 5, 30),
                    new DateTime(2026, 5, 30, 7, 0, 0, DateTimeKind.Utc),
                    TaskPriority.High,
                    null)
            ]);
        _voiceParsing.ParseAudioAsync(
                Arg.Any<Stream>(),
                "recording.m4a",
                household.Id,
                Arg.Any<IReadOnlyList<TaskVoiceHouseholdMember>>(),
                "Europe/Madrid",
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);
        await using var audio = new MemoryStream([1, 2, 3]);

        var result = await new ParseTaskVoiceAudioCommandHandler(
                _listRepository,
                _householdRepository,
                _currentUser,
                _voiceParsing,
                _taskRepository,
                _textNormalizer,
                _userRepository)
            .Handle(
                new ParseTaskVoiceAudioCommand(
                    list.Id,
                    audio,
                    "recording.m4a",
                    "Europe/Madrid",
                    new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.FromHours(2))),
                CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tasks.Should().ContainSingle(task => task.Title == "Limpiar la cocina");
        result.Value.Tasks.Should().ContainSingle(task => task.AssignedToUserName == "Marina");
    }

    [Fact]
    public async Task Handle_WhenParsedTaskAlreadyExists_ReturnsMatchedExistingTask()
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Chores", ListType.Tasks, household.Id, _userId);
        var existingTask = new TaskItem("Limpiar la cocina", list.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        _taskRepository.GetByListIdAsync(
                list.Id,
                "All",
                null,
                null,
                null,
                Arg.Any<CancellationToken>())
            .Returns([existingTask]);
        _voiceParsing.ParseAudioAsync(
                Arg.Any<Stream>(),
                "recording.m4a",
                household.Id,
                Arg.Any<IReadOnlyList<TaskVoiceHouseholdMember>>(),
                "Europe/Madrid",
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(new TaskVoiceParsingResult(
                "limpia la cocina",
                [
                    new ParsedTaskDto(
                        "Limpiar la cocina",
                        null,
                        null,
                        null,
                        null,
                        TaskPriority.Normal,
                        null)
                ]));
        await using var audio = new MemoryStream([1, 2, 3]);

        var result = await new ParseTaskVoiceAudioCommandHandler(
                _listRepository,
                _householdRepository,
                _currentUser,
                _voiceParsing,
                _taskRepository,
                _textNormalizer)
            .Handle(
                new ParseTaskVoiceAudioCommand(
                    list.Id,
                    audio,
                    "recording.m4a",
                    "Europe/Madrid",
                    new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.FromHours(2))),
                CancellationToken.None);

        result.Value!.Tasks.Should().ContainSingle(task => task.MatchedExistingTask == "Limpiar la cocina");
    }

    [Fact]
    public async Task Handle_WithShoppingList_ReturnsValidationFailure()
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);
        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>()).Returns(list);
        await using var audio = new MemoryStream([1, 2, 3]);

        var result = await new ParseTaskVoiceAudioCommandHandler(
                _listRepository,
                _householdRepository,
                _currentUser,
                _voiceParsing,
                _taskRepository,
                _textNormalizer)
            .Handle(
                new ParseTaskVoiceAudioCommand(
                    list.Id,
                    audio,
                    "recording.m4a",
                    "Europe/Madrid",
                    new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.FromHours(2))),
                CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
        await _voiceParsing.DidNotReceive().ParseAudioAsync(
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyList<TaskVoiceHouseholdMember>>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    private static User CreateUser(Guid id, string displayName)
    {
        var user = new User($"firebase-{id}", displayName, $"{displayName}@example.com");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(user, id);
        return user;
    }
}
