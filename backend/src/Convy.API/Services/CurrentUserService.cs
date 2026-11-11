using Convy.Application.Common.Interfaces;
using Convy.Domain.Repositories;
using System.Security.Claims;

namespace Convy.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;
    private Guid? _resolvedUserId;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
    }

    public Guid UserId
    {
        get
        {
            return _resolvedUserId ?? Guid.Empty;
        }
    }

    public string FirebaseUid
    {
        get => GetFirebaseUid(_httpContextAccessor.HttpContext?.User);
    }

    public Task ResolveAsync(CancellationToken cancellationToken = default) =>
        ResolveAsync(_httpContextAccessor.HttpContext?.User, cancellationToken);

    public async Task ResolveAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken = default)
    {
        if (_resolvedUserId.HasValue || principal?.Identity?.IsAuthenticated != true)
            return;

        if (string.Equals(principal.FindFirst("auth_source")?.Value, "mcp", StringComparison.Ordinal)
            && Guid.TryParse(principal.FindFirst("sub")?.Value, out var mcpUserId))
        {
            _resolvedUserId = mcpUserId;
            return;
        }

        var firebaseUid = GetFirebaseUid(principal);
        if (string.IsNullOrEmpty(firebaseUid))
            return;

        var user = await _userRepository.GetByFirebaseUidAsync(firebaseUid, cancellationToken);
        _resolvedUserId = user?.Id ?? Guid.Empty;
    }

    private static string GetFirebaseUid(ClaimsPrincipal? principal)
    {
        if (string.Equals(principal?.FindFirst("auth_source")?.Value, "mcp", StringComparison.Ordinal))
            return string.Empty;

        return principal?.FindFirst("user_id")?.Value ?? principal?.FindFirst("sub")?.Value ?? string.Empty;
    }
}
