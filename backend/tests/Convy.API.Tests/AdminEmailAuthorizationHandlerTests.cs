using System.Security.Claims;
using Convy.API.Authorization;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Convy.API.Tests;

public class AdminEmailAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithAllowlistedFirebaseEmailAndNoLocalUser_Succeeds()
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByFirebaseUidAsync("firebase-admin", Arg.Any<CancellationToken>())
            .Returns((User?)null);
        var handler = CreateHandler(userRepository, "admin@example.com");
        var context = CreateContext("firebase-admin", "admin@example.com");

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithAllowlistedLocalUserEmail_Succeeds()
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByFirebaseUidAsync("firebase-admin", Arg.Any<CancellationToken>())
            .Returns(new User("firebase-admin", "Admin", "admin@example.com"));
        var handler = CreateHandler(userRepository, "admin@example.com");
        var context = CreateContext("firebase-admin", "other@example.com");

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithNonAllowlistedEmail_Fails()
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByFirebaseUidAsync("firebase-admin", Arg.Any<CancellationToken>())
            .Returns(new User("firebase-admin", "Admin", "other@example.com"));
        var handler = CreateHandler(userRepository, "admin@example.com");
        var context = CreateContext("firebase-admin", "other@example.com");

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    private static AdminEmailAuthorizationHandler CreateHandler(IUserRepository userRepository, string allowedEmails)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:AllowedEmails"] = allowedEmails,
            })
            .Build();

        return new AdminEmailAuthorizationHandler(userRepository, configuration);
    }

    private static AuthorizationHandlerContext CreateContext(string firebaseUid, string email)
    {
        var requirement = new AdminEmailRequirement();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("user_id", firebaseUid),
                new Claim("email", email),
            ],
            "Firebase"));

        return new AuthorizationHandlerContext([requirement], principal, resource: null);
    }
}
