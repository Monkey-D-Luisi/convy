using Convy.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Convy.API.Authorization;

public class AdminEmailRequirement : IAuthorizationRequirement;

public class AdminEmailAuthorizationHandler : AuthorizationHandler<AdminEmailRequirement>
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AdminEmailAuthorizationHandler(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminEmailRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (string.Equals(context.User.FindFirst("auth_source")?.Value, "mcp", StringComparison.Ordinal))
            return;

        var allowedEmails = GetAllowedEmails();
        if (allowedEmails.Count == 0)
            return;

        var firebaseEmail = context.User.FindFirst("email")?.Value
            ?? context.User.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrWhiteSpace(firebaseEmail) && allowedEmails.Contains(firebaseEmail))
        {
            context.Succeed(requirement);
            return;
        }

        var firebaseUid = context.User.FindFirst("user_id")?.Value
            ?? context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(firebaseUid))
            return;

        var user = await _userRepository.GetByFirebaseUidAsync(firebaseUid);
        if (user is not null && allowedEmails.Contains(user.Email))
            context.Succeed(requirement);
    }

    private HashSet<string> GetAllowedEmails()
    {
        var values = _configuration.GetSection("Admin:AllowedEmails").Get<string[]>()
            ?? SplitCommaSeparated(_configuration["Admin:AllowedEmails"]);

        return values
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string[] SplitCommaSeparated(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
