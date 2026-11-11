using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Convy.API.Services;
using Convy.Domain.Entities;
using Convy.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Convy.API.Tests;

public class McpResourceMigrationTests
{
    private const string Legacy = "https://mcp.convyapp.com";
    private const string Alias = "https://mcp-convy.monkeydluisi.com";
    private const string Client = "https://chatgpt.com/aip/g-test/.well-known/oauth-client";

    [Theory]
    [InlineData(Legacy)]
    [InlineData(Alias)]
    public async Task RedeemAuthorizationCodeAsync_BindsAudienceAndRefreshToOriginalResource(string resource)
    {
        using var key = RSA.Create(2048);
        var config = Configuration(key);
        await using var db = Context();
        var user = Guid.NewGuid();
        const string verifier = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJK";
        var challenge = Base64UrlEncoder.Encode(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));
        var service = new McpOAuthService(db, new McpTokenService(config), config);
        var approval = await service.ApproveAsync(new(Client, "https://chatgpt.com/callback", resource,
            ["convy.households.read"], "state", challenge, "S256"), user, default);
        approval.IsValid.Should().BeTrue();
        var code = Uri.UnescapeDataString(approval.RedirectUri!.Split("code=")[1].Split('&')[0]);
        var wrongResource = resource == Legacy ? Alias : Legacy;
        var wrong = await service.RedeemAuthorizationCodeAsync(code, Client, "https://chatgpt.com/callback", wrongResource, verifier, default);
        wrong.IsValid.Should().BeFalse();
        var redeemed = await service.RedeemAuthorizationCodeAsync(code, Client, "https://chatgpt.com/callback", resource, verifier, default);
        redeemed.IsValid.Should().BeTrue();
        AssertIdentity(redeemed.Token!.AccessToken, resource);
        (await service.RedeemAuthorizationCodeAsync(code, Client, "https://chatgpt.com/callback", resource, verifier, default)).IsValid.Should().BeFalse();
        (await service.RedeemRefreshTokenAsync(redeemed.Token.RefreshToken, Client, wrongResource, default)).IsValid.Should().BeFalse();
        var refreshed = await service.RedeemRefreshTokenAsync(redeemed.Token.RefreshToken, Client, resource, default);
        refreshed.IsValid.Should().BeTrue();
        AssertIdentity(refreshed.Token!.AccessToken, resource);
        (await service.RedeemRefreshTokenAsync(redeemed.Token.RefreshToken, Client, resource, default)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemRefreshTokenAsync_PreMigrationRecordKeepsLegacyIdentity()
    {
        using var key = RSA.Create(2048);
        var config = Configuration(key);
        await using var db = Context();
        db.McpOAuthRefreshTokens.Add(new McpOAuthRefreshToken(McpOAuthService.HashToken("old-refresh"), Guid.NewGuid(), Client,
            Legacy, "convy.households.read", DateTime.UtcNow.AddDays(1)));
        await db.SaveChangesAsync();
        var service = new McpOAuthService(db, new McpTokenService(config), config);
        var result = await service.RedeemRefreshTokenAsync("old-refresh", Client, Legacy, default);
        result.IsValid.Should().BeTrue();
        AssertIdentity(result.Token!.AccessToken, Legacy);
        service.IsExpectedResource("https://attacker.example").Should().BeFalse();
        service.IsExpectedResource(Alias + "/").Should().BeFalse();
        var issueUnknown = () => new McpTokenService(config).CreateAccessToken(Guid.NewGuid(), Client, "convy.households.read", "https://attacker.example");
        issueUnknown.Should().Throw<ArgumentException>();
    }

    private static void AssertIdentity(string token, string resource)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be("https://auth.convyapp.com");
        jwt.Audiences.Should().Equal(resource);
    }

    private static IConfiguration Configuration(RSA key) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["McpAuth:Issuer"] = "https://auth.convyapp.com",
        ["McpAuth:Audience"] = Legacy,
        ["McpAuth:AdditionalAudiences:0"] = Alias,
        ["McpAuth:PrivateKeyPem"] = key.ExportPkcs8PrivateKeyPem(),
    }).Build();

    private static ConvyDbContext Context() => new(new DbContextOptionsBuilder<ConvyDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
