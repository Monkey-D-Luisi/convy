using System.Security.Cryptography;
using System.Text;
using Convy.API.Authorization;
using Convy.Domain.Entities;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Convy.API.Services;

public class McpOAuthService
{
    private readonly ConvyDbContext _context;
    private readonly McpTokenService _tokenService;
    private readonly IConfiguration _configuration;

    public McpOAuthService(ConvyDbContext context, McpTokenService tokenService, IConfiguration configuration)
    {
        _context = context;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<McpAuthorizationApprovalResult> ApproveAsync(
        McpAuthorizationApprovalRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var scopeResult = NormalizeScopes(request.Scopes);
        if (!scopeResult.IsValid)
            return McpAuthorizationApprovalResult.Invalid(scopeResult.Error!);

        if (!IsExpectedResource(request.Resource))
            return McpAuthorizationApprovalResult.Invalid("invalid_resource");

        if (!string.Equals(request.CodeChallengeMethod, "S256", StringComparison.Ordinal))
            return McpAuthorizationApprovalResult.Invalid("invalid_code_challenge_method");

        var code = CreateOpaqueToken();
        var codeHash = HashToken(code);
        var expiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue("McpAuth:AuthorizationCodeMinutes", 5));

        _context.McpOAuthConsents.Add(new McpOAuthConsent(userId, request.ClientId, request.Resource, scopeResult.Scopes!));
        _context.McpOAuthAuthorizationCodes.Add(new McpOAuthAuthorizationCode(
            codeHash,
            userId,
            request.ClientId,
            request.RedirectUri,
            request.Resource,
            scopeResult.Scopes!,
            request.CodeChallenge,
            request.CodeChallengeMethod,
            expiresAt));

        await _context.SaveChangesAsync(cancellationToken);

        var redirectUri = AppendOAuthQuery(request.RedirectUri, code, request.State);
        return McpAuthorizationApprovalResult.Valid(redirectUri);
    }

    public async Task<McpTokenEndpointResult> RedeemAuthorizationCodeAsync(
        string code,
        string clientId,
        string redirectUri,
        string resource,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
                : null;
            var codeHash = HashToken(code);
            var record = await _context.McpOAuthAuthorizationCodes
                .FirstOrDefaultAsync(item => item.CodeHash == codeHash, cancellationToken);

            if (record is null)
                return McpTokenEndpointResult.Invalid("invalid_grant");
            if (record.UsedAt.HasValue || record.IsExpired(DateTime.UtcNow))
                return McpTokenEndpointResult.Invalid("invalid_grant");
            if (!string.Equals(record.ClientId, clientId, StringComparison.Ordinal)
                || !string.Equals(record.RedirectUri, redirectUri, StringComparison.Ordinal)
                || !string.Equals(record.Resource, resource, StringComparison.Ordinal))
            {
                return McpTokenEndpointResult.Invalid("invalid_grant");
            }
            if (!VerifyPkce(codeVerifier, record.CodeChallenge))
                return McpTokenEndpointResult.Invalid("invalid_grant");

            record.MarkUsed(DateTime.UtcNow);
            var refreshToken = CreateOpaqueToken();
            _context.McpOAuthRefreshTokens.Add(new McpOAuthRefreshToken(
                HashToken(refreshToken),
                record.UserId,
                record.ClientId,
                record.Resource,
                record.Scopes,
                DateTime.UtcNow.AddDays(_configuration.GetValue("McpAuth:RefreshTokenDays", 30))));

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return CreateTokenResult(record.UserId, record.ClientId, record.Scopes, refreshToken);
        });
    }

    public async Task<McpTokenEndpointResult> RedeemRefreshTokenAsync(
        string refreshToken,
        string clientId,
        string resource,
        CancellationToken cancellationToken)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
                : null;
            var tokenHash = HashToken(refreshToken);
            var record = await _context.McpOAuthRefreshTokens
                .FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

            if (record is null
                || record.RevokedAt.HasValue
                || record.IsExpired(DateTime.UtcNow)
                || !string.Equals(record.ClientId, clientId, StringComparison.Ordinal)
                || !string.Equals(record.Resource, resource, StringComparison.Ordinal))
            {
                return McpTokenEndpointResult.Invalid("invalid_grant");
            }

            var replacementRefreshToken = CreateOpaqueToken();
            var replacementHash = HashToken(replacementRefreshToken);
            record.MarkUsed(DateTime.UtcNow);
            record.RotateTo(replacementHash, DateTime.UtcNow);

            _context.McpOAuthRefreshTokens.Add(new McpOAuthRefreshToken(
                replacementHash,
                record.UserId,
                record.ClientId,
                record.Resource,
                record.Scopes,
                DateTime.UtcNow.AddDays(_configuration.GetValue("McpAuth:RefreshTokenDays", 30))));

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return CreateTokenResult(record.UserId, record.ClientId, record.Scopes, replacementRefreshToken);
        });
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);
        var record = await _context.McpOAuthRefreshTokens
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (record is null)
            return;

        record.Revoke(DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public bool IsExpectedResource(string resource) =>
        string.Equals(resource, _configuration["McpAuth:Audience"] ?? "https://mcp.convyapp.com", StringComparison.Ordinal);

    public static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Base64UrlEncoder.Encode(hash);
    }

    private McpTokenEndpointResult CreateTokenResult(Guid userId, string clientId, string scopes, string refreshToken)
    {
        var accessToken = _tokenService.CreateAccessToken(userId, clientId, scopes);
        return McpTokenEndpointResult.Valid(new McpTokenResponse(
            accessToken,
            "Bearer",
            _configuration.GetValue("McpAuth:AccessTokenMinutes", 60) * 60,
            refreshToken,
            scopes));
    }

    private static McpScopeNormalizationResult NormalizeScopes(IEnumerable<string> scopes)
    {
        var requested = scopes
            .SelectMany(scope => scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (requested.Length == 0)
            return McpScopeNormalizationResult.Invalid("invalid_scope");

        if (requested.Any(scope => !McpScopes.Supported.Contains(scope, StringComparer.Ordinal)))
            return McpScopeNormalizationResult.Invalid("invalid_scope");

        return McpScopeNormalizationResult.Valid(string.Join(' ', requested));
    }

    private static bool VerifyPkce(string codeVerifier, string expectedChallenge)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var actualChallenge = Base64UrlEncoder.Encode(hash);
        return string.Equals(actualChallenge, expectedChallenge, StringComparison.Ordinal);
    }

    private static string CreateOpaqueToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    private static string AppendOAuthQuery(string redirectUri, string code, string? state)
    {
        var separator = redirectUri.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var builder = new StringBuilder()
            .Append(redirectUri)
            .Append(separator)
            .Append("code=")
            .Append(Uri.EscapeDataString(code));

        if (!string.IsNullOrWhiteSpace(state))
        {
            builder.Append("&state=")
                .Append(Uri.EscapeDataString(state));
        }

        return builder.ToString();
    }
}

public record McpAuthorizationApprovalRequest(
    string ClientId,
    string RedirectUri,
    string Resource,
    IReadOnlyList<string> Scopes,
    string? State,
    string CodeChallenge,
    string CodeChallengeMethod);

public record McpAuthorizationApprovalResult(bool IsValid, string? RedirectUri, string? Error)
{
    public static McpAuthorizationApprovalResult Valid(string redirectUri) => new(true, redirectUri, null);
    public static McpAuthorizationApprovalResult Invalid(string error) => new(false, null, error);
}

public record McpTokenEndpointResult(bool IsValid, McpTokenResponse? Token, string? Error)
{
    public static McpTokenEndpointResult Valid(McpTokenResponse token) => new(true, token, null);
    public static McpTokenEndpointResult Invalid(string error) => new(false, null, error);
}

public record McpTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string RefreshToken,
    string Scope);

internal record McpScopeNormalizationResult(bool IsValid, string? Scopes, string? Error)
{
    public static McpScopeNormalizationResult Valid(string scopes) => new(true, scopes, null);
    public static McpScopeNormalizationResult Invalid(string error) => new(false, null, error);
}
