using Convy.Application.Common.Interfaces;
using Convy.Application.Features.Users.Commands;
using Convy.Application.Features.Users.Queries;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace Convy.Application.Tests.Features.Users;

public class NotificationPreferencesHandlerTests
{
    private readonly INotificationPreferencesRepository _repository = Substitute.For<INotificationPreferencesRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly Guid _userId = Guid.NewGuid();

    public NotificationPreferencesHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
    }

    [Fact]
    public async Task GetNotificationPreferences_WhenMissing_ReturnsDefaultPreferences()
    {
        _repository.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((NotificationPreferences?)null);
        var handler = new GetNotificationPreferencesQueryHandler(_repository, _currentUser);

        var result = await handler.Handle(new GetNotificationPreferencesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ItemsAdded.Should().BeTrue();
        result.Value.TasksAdded.Should().BeTrue();
        result.Value.ItemsCompleted.Should().BeFalse();
        result.Value.TasksCompleted.Should().BeFalse();
        result.Value.ItemTaskChanges.Should().BeFalse();
        result.Value.ListChanges.Should().BeTrue();
        result.Value.MemberChanges.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WhenMissing_CreatesPreferenceRow()
    {
        _repository.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((NotificationPreferences?)null);
        var handler = new UpdateNotificationPreferencesCommandHandler(_repository, _currentUser);
        var command = new UpdateNotificationPreferencesCommand(
            ItemsAdded: false,
            TasksAdded: true,
            ItemsCompleted: true,
            TasksCompleted: false,
            ItemTaskChanges: true,
            ListChanges: false,
            MemberChanges: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<NotificationPreferences>(preferences =>
                preferences.UserId == _userId &&
                !preferences.ItemsAdded &&
                preferences.TasksAdded &&
                preferences.ItemsCompleted &&
                !preferences.TasksCompleted &&
                preferences.ItemTaskChanges &&
                !preferences.ListChanges &&
                preferences.MemberChanges),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WhenExisting_UpdatesPreferenceRow()
    {
        var existing = NotificationPreferences.CreateDefault(_userId);
        _repository.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(existing);
        var handler = new UpdateNotificationPreferencesCommandHandler(_repository, _currentUser);

        var result = await handler.Handle(
            new UpdateNotificationPreferencesCommand(false, false, true, true, true, false, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.ItemsAdded.Should().BeFalse();
        existing.TasksAdded.Should().BeFalse();
        existing.ItemsCompleted.Should().BeTrue();
        existing.TasksCompleted.Should().BeTrue();
        existing.ItemTaskChanges.Should().BeTrue();
        existing.ListChanges.Should().BeFalse();
        existing.MemberChanges.Should().BeFalse();
        await _repository.DidNotReceive().AddAsync(Arg.Any<NotificationPreferences>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
