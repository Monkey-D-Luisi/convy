namespace Convy.API.Authorization;

public static class McpPolicyNames
{
    public static string OnlyScope(string scope) => $"McpOnly:{scope}";
}
