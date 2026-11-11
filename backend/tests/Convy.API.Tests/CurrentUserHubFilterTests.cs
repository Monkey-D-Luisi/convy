using System.Security.Claims;
using Convy.API.Services;
using Convy.Application.Common.Interfaces;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Hubs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Convy.API.Tests;

public class CurrentUserHubFilterTests
{
    [Theory]
    [InlineData("JoinHousehold", false)]
    [InlineData("LeaveHousehold", false)]
    [InlineData("JoinHousehold", true)]
    [InlineData("LeaveHousehold", true)]
    public async Task InvokeMethodAsync_MemberWithNewInvocationScope_ResolvesIdentityAndUpdatesGroup(
        string methodName, bool mcpIdentity)
    {
        var user = new User("firebase-member", "Member", "member@example.invalid");
        var household = new Household("Test household", user.Id);
        var principal = AuthenticatedPrincipal(user, mcpIdentity);
        var users = Substitute.For<IUserRepository>();
        users.GetByFirebaseUidAsync(user.FirebaseUid, Arg.Any<CancellationToken>()).Returns(user);
        var households = Substitute.For<IHouseholdRepository>();
        households.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        using var provider = CreateProvider(users, households);
        using var scope = provider.CreateScope();
        var hub = CreateHub(scope.ServiceProvider, principal);
        scope.ServiceProvider.GetRequiredService<CurrentUserService>().UserId.Should().BeEmpty();

        await InvokeAsync(scope.ServiceProvider, hub, household.Id, methodName);

        scope.ServiceProvider.GetRequiredService<CurrentUserService>().UserId.Should().Be(user.Id);
        if (methodName == "JoinHousehold")
            await hub.Groups.Received(1).AddToGroupAsync("test-connection", household.Id.ToString(), Arg.Any<CancellationToken>());
        else
            await hub.Groups.Received(1).RemoveFromGroupAsync("test-connection", household.Id.ToString(), Arg.Any<CancellationToken>());
        if (mcpIdentity)
            await users.DidNotReceiveWithAnyArgs().GetByFirebaseUidAsync(default!, default);
    }

    [Theory]
    [InlineData("JoinHousehold")]
    [InlineData("LeaveHousehold")]
    public async Task InvokeMethodAsync_AuthenticatedOutsider_RejectsGroupAccess(string methodName)
    {
        var user = new User("firebase-outsider", "Outsider", "outsider@example.invalid");
        var household = new Household("Private household", Guid.NewGuid());
        var users = Substitute.For<IUserRepository>();
        users.GetByFirebaseUidAsync(user.FirebaseUid, Arg.Any<CancellationToken>()).Returns(user);
        var households = Substitute.For<IHouseholdRepository>();
        households.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        using var provider = CreateProvider(users, households);
        using var scope = provider.CreateScope();
        var hub = CreateHub(scope.ServiceProvider, AuthenticatedPrincipal(user, false));

        var action = () => InvokeAsync(scope.ServiceProvider, hub, household.Id, methodName);

        await action.Should().ThrowAsync<HubException>().WithMessage("Forbidden");
        await hub.Groups.DidNotReceiveWithAnyArgs().AddToGroupAsync(default!, default!, default);
        await hub.Groups.DidNotReceiveWithAnyArgs().RemoveFromGroupAsync(default!, default!, default);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvokeMethodAsync_UnknownOrUnauthenticatedUser_RejectsGroupAccess(bool authenticated)
    {
        var user = new User("unknown-firebase", "Unknown", "unknown@example.invalid");
        var principal = authenticated ? AuthenticatedPrincipal(user, false) : new ClaimsPrincipal(new ClaimsIdentity());
        var household = new Household("Private household", Guid.NewGuid());
        var users = Substitute.For<IUserRepository>();
        var households = Substitute.For<IHouseholdRepository>();
        households.GetByIdWithMembersAsync(household.Id, Arg.Any<CancellationToken>()).Returns(household);
        using var provider = CreateProvider(users, households);
        using var scope = provider.CreateScope();
        var hub = CreateHub(scope.ServiceProvider, principal);

        var action = () => InvokeAsync(scope.ServiceProvider, hub, household.Id, "JoinHousehold");

        await action.Should().ThrowAsync<HubException>().WithMessage("Forbidden");
        await hub.Groups.DidNotReceiveWithAnyArgs().AddToGroupAsync(default!, default!, default);
    }

    private static ServiceProvider CreateProvider(IUserRepository users, IHouseholdRepository households) =>
        new ServiceCollection()
            .AddSingleton(users)
            .AddSingleton(households)
            .AddSingleton<IHttpContextAccessor>(new HttpContextAccessor())
            .AddScoped<CurrentUserService>()
            .AddScoped<ICurrentUserService>(services => services.GetRequiredService<CurrentUserService>())
            .BuildServiceProvider();

    private static HouseholdHub CreateHub(IServiceProvider services, ClaimsPrincipal principal)
    {
        var context = Substitute.For<HubCallerContext>();
        context.User.Returns(principal);
        context.ConnectionId.Returns("test-connection");
        return new HouseholdHub(services.GetRequiredService<IHouseholdRepository>(), services.GetRequiredService<ICurrentUserService>())
        {
            Context = context,
            Groups = Substitute.For<IGroupManager>()
        };
    }

    private static ClaimsPrincipal AuthenticatedPrincipal(User user, bool mcpIdentity) => new(new ClaimsIdentity(
        mcpIdentity
            ? [new Claim("sub", user.Id.ToString()), new Claim("auth_source", "mcp")]
            : [new Claim("user_id", user.FirebaseUid)],
        "validated-test-identity"));

    private static async Task InvokeAsync(IServiceProvider services, HouseholdHub hub, Guid householdId, string methodName)
    {
        var invocation = new HubInvocationContext(hub.Context, services, hub,
            typeof(HouseholdHub).GetMethod(methodName)!, [householdId]);
        await new CurrentUserHubFilter().InvokeMethodAsync(invocation, async _ =>
        {
            if (methodName == "JoinHousehold")
                await hub.JoinHousehold(householdId);
            else
                await hub.LeaveHousehold(householdId);
            return null;
        });
    }
}
