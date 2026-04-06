using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Items.Commands;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Items;

public class ParseVoiceAudioCommandHandlerTests
{
    private readonly IHouseholdListRepository _listRepository = Substitute.For<IHouseholdListRepository>();
    private readonly IHouseholdRepository _householdRepository = Substitute.For<IHouseholdRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IAiVoiceParsingService _voiceParsingService = Substitute.For<IAiVoiceParsingService>();
    private readonly ParseVoiceAudioCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public ParseVoiceAudioCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new ParseVoiceAudioCommandHandler(
            _listRepository,
            _householdRepository,
            _currentUser,
            _voiceParsingService);
    }

    private Guid SetupValidListAndHousehold()
    {
        var household = new Household("Home", _userId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, _userId);

        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>())
            .Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        return list.Id;
    }

    [Fact]
    public async Task Handle_WithValidAudio_ReturnsVoiceParsingResult()
    {
        // Arrange
        var listId = SetupValidListAndHousehold();
        var audio = new MemoryStream([1, 2, 3]);
        var command = new ParseVoiceAudioCommand(listId, audio, "test.m4a");

        var expected = new VoiceParsingResult("milk and bread", [
            new ParsedItemDto("Milk", null, null, null),
            new ParsedItemDto("Bread", null, null, null),
        ]);
        _voiceParsingService
            .ParseAudioAsync(audio, "test.m4a", Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Transcription.Should().Be("milk and bread");
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenListNotFound_ReturnsNotFound()
    {
        // Arrange
        _listRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdList?)null);

        var command = new ParseVoiceAudioCommand(Guid.NewGuid(), new MemoryStream(), "test.m4a");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
        await _voiceParsingService.DidNotReceive()
            .ParseAudioAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotMember_ReturnsForbidden()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var household = new Household("Home", otherUserId);
        var list = new HouseholdList("Shopping", ListType.Shopping, household.Id, otherUserId);

        _listRepository.GetByIdAsync(list.Id, Arg.Any<CancellationToken>())
            .Returns(list);
        _householdRepository.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(household);

        var command = new ParseVoiceAudioCommand(list.Id, new MemoryStream(), "test.m4a");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Forbidden");
        await _voiceParsingService.DidNotReceive()
            .ParseAudioAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CallsServiceWithCorrectHouseholdId()
    {
        // Arrange
        var listId = SetupValidListAndHousehold();
        var audio = new MemoryStream([1, 2, 3]);
        var command = new ParseVoiceAudioCommand(listId, audio, "recording.m4a");

        _voiceParsingService
            .ParseAudioAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new VoiceParsingResult("test", []));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _voiceParsingService.Received(1)
            .ParseAudioAsync(audio, "recording.m4a", Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
