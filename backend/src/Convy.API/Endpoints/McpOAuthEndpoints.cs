using Convy.API.Authorization;
using Convy.API.Services;
using Convy.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Convy.API.Endpoints;

public static class McpOAuthEndpoints
{
    public static void MapMcpOAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/.well-known/oauth-protected-resource", (IConfiguration configuration) =>
            Results.Json(CreateProtectedResourceMetadata(configuration)))
            .AllowAnonymous();

        routes.MapGet("/.well-known/oauth-authorization-server", (IConfiguration configuration) =>
            Results.Json(CreateAuthorizationServerMetadata(configuration)))
            .AllowAnonymous();

        routes.MapGet("/.well-known/openid-configuration", (IConfiguration configuration) =>
            Results.Json(CreateAuthorizationServerMetadata(configuration)))
            .AllowAnonymous();

        routes.MapPost("/api/v1/mcp/oauth/authorize/approve", [Authorize(AuthenticationSchemes = AuthSchemes.FirebaseBearer)] async (
            McpAuthorizeApproveRequest request,
            ICurrentUserService currentUser,
            McpClientMetadataValidator clientMetadataValidator,
            McpOAuthService oauthService,
            CancellationToken cancellationToken) =>
        {
            var metadata = await clientMetadataValidator.ValidateAsync(request.ClientId, request.RedirectUri, cancellationToken);
            if (!metadata.IsValid)
                return Results.BadRequest(new { error = metadata.Error });

            var result = await oauthService.ApproveAsync(
                new McpAuthorizationApprovalRequest(
                    request.ClientId,
                    request.RedirectUri,
                    request.Resource,
                    request.Scopes,
                    request.State,
                    request.CodeChallenge,
                    request.CodeChallengeMethod),
                currentUser.UserId,
                cancellationToken);

            return result.IsValid
                ? Results.Ok(new { redirectUri = result.RedirectUri })
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireRateLimiting("mcp-oauth");

        routes.MapPost("/oauth/token", async (
            HttpRequest request,
            McpOAuthService oauthService,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { error = "invalid_request" });

            var form = await request.ReadFormAsync(cancellationToken);
            var grantType = form["grant_type"].ToString();
            McpTokenEndpointResult result;

            if (grantType == "authorization_code")
            {
                result = await oauthService.RedeemAuthorizationCodeAsync(
                    form["code"].ToString(),
                    form["client_id"].ToString(),
                    form["redirect_uri"].ToString(),
                    form["resource"].ToString(),
                    form["code_verifier"].ToString(),
                    cancellationToken);
            }
            else if (grantType == "refresh_token")
            {
                result = await oauthService.RedeemRefreshTokenAsync(
                    form["refresh_token"].ToString(),
                    form["client_id"].ToString(),
                    form["resource"].ToString(),
                    cancellationToken);
            }
            else
            {
                return Results.BadRequest(new { error = "unsupported_grant_type" });
            }

            return result.IsValid
                ? Results.Json(new Dictionary<string, object?>
                {
                    ["access_token"] = result.Token!.AccessToken,
                    ["token_type"] = result.Token.TokenType,
                    ["expires_in"] = result.Token.ExpiresIn,
                    ["refresh_token"] = result.Token.RefreshToken,
                    ["scope"] = result.Token.Scope,
                })
                : Results.BadRequest(new { error = result.Error });
        })
        .AllowAnonymous()
        .RequireRateLimiting("mcp-oauth");

        routes.MapPost("/oauth/revoke", async (
            HttpRequest request,
            McpOAuthService oauthService,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { error = "invalid_request" });

            var form = await request.ReadFormAsync(cancellationToken);
            await oauthService.RevokeRefreshTokenAsync(form["token"].ToString(), cancellationToken);
            return Results.Ok();
        })
        .AllowAnonymous()
        .RequireRateLimiting("mcp-oauth");
    }

    private static Dictionary<string, object?> CreateProtectedResourceMetadata(IConfiguration configuration)
    {
        var resource = configuration["McpAuth:Audience"] ?? "https://mcp.convy.app";
        var issuer = configuration["McpAuth:Issuer"] ?? "https://auth.convy.app";

        return new Dictionary<string, object?>
        {
            ["resource"] = resource,
            ["authorization_servers"] = new[] { issuer },
            ["scopes_supported"] = McpScopes.Supported,
            ["bearer_methods_supported"] = new[] { "header" },
            ["resource_documentation"] = $"{resource.TrimEnd('/')}/docs",
        };
    }

    private static Dictionary<string, object?> CreateAuthorizationServerMetadata(IConfiguration configuration)
    {
        var issuer = configuration["McpAuth:Issuer"] ?? "https://auth.convy.app";
        var authorizationEndpoint = configuration["McpAuth:AuthorizationEndpoint"] ?? $"{issuer.TrimEnd('/')}/oauth/authorize";

        return new Dictionary<string, object?>
        {
            ["issuer"] = issuer,
            ["authorization_endpoint"] = authorizationEndpoint,
            ["token_endpoint"] = $"{issuer.TrimEnd('/')}/oauth/token",
            ["revocation_endpoint"] = $"{issuer.TrimEnd('/')}/oauth/revoke",
            ["response_types_supported"] = new[] { "code" },
            ["grant_types_supported"] = new[] { "authorization_code", "refresh_token" },
            ["code_challenge_methods_supported"] = new[] { "S256" },
            ["token_endpoint_auth_methods_supported"] = new[] { "none" },
            ["client_id_metadata_document_supported"] = true,
            ["scopes_supported"] = McpScopes.Supported,
        };
    }
}

public record McpAuthorizeApproveRequest(
    string ClientId,
    string RedirectUri,
    string Resource,
    IReadOnlyList<string> Scopes,
    string? State,
    string CodeChallenge,
    string CodeChallengeMethod);
