using Convy.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;

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

        var allowedEmails = GetAllowedEmails();
        if (allowedEmails.Count == 0)
            return;

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
