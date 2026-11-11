namespace Convy.API.Services;

public static class McpResources
{
    public static string[] GetAudiences(IConfiguration configuration) =>
        new[] { configuration["McpAuth:Audience"] ?? "https://mcp.convyapp.com" }
            .Concat(configuration.GetSection("McpAuth:AdditionalAudiences").Get<string[]>() ?? [])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
