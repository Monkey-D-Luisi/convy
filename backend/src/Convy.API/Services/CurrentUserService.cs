using Convy.Application.Common.Interfaces;
using Convy.Domain.Repositories;

namespace Convy.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;
    private Guid? _cachedUserId;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
    }

    public Guid UserId
    {
        get
        {
            if (_cachedUserId.HasValue)
                return _cachedUserId.Value;

            // Fallback: sync-over-async for cases where middleware didn't run.
            // Safe in ASP.NET Core (no SynchronizationContext) but prefer ResolveAsync via middleware.
            var firebaseUid = FirebaseUid;
            var user = _userRepository.GetByFirebaseUidAsync(firebaseUid).GetAwaiter().GetResult();

            _cachedUserId = user?.Id ?? Guid.Empty;
            return _cachedUserId.Value;
        }
    }

    public string FirebaseUid
    {
        get
        {
            var uid = _httpContextAccessor.HttpContext?.User?.FindFirst("user_id")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

            return uid ?? string.Empty;
        }
    }

    public async Task ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedUserId.HasValue)
            return;

        var firebaseUid = FirebaseUid;
        if (string.IsNullOrEmpty(firebaseUid))
            return;

        var user = await _userRepository.GetByFirebaseUidAsync(firebaseUid, cancellationToken);
        _cachedUserId = user?.Id ?? Guid.Empty;
    }
}
