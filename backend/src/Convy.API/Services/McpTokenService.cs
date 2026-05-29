using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Convy.API.Services;

public class McpTokenService
{
    private readonly IConfiguration _configuration;

    public McpTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateAccessToken(Guid userId, string clientId, string scopes)
    {
        var privateKey = McpJwtKeyLoader.LoadPrivateKey(_configuration)
            ?? throw new InvalidOperationException("McpAuth private signing key is not configured.");

        var issuer = _configuration["McpAuth:Issuer"] ?? "https://auth.convyapp.com";
        var audience = _configuration["McpAuth:Audience"] ?? "https://mcp.convyapp.com";
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_configuration.GetValue("McpAuth:AccessTokenMinutes", 60));

        var credentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("client_id", clientId),
            new("scope", scopes),
            new("token_use", "mcp_access"),
            new("auth_source", "mcp"),
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
