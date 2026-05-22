using Convy.Application.Common.Interfaces;
using Convy.Domain.Repositories;

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
        get
        {
            var uid = _httpContextAccessor.HttpContext?.User?.FindFirst("user_id")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

            return uid ?? string.Empty;
        }
    }

    public async Task ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (_resolvedUserId.HasValue)
            return;

        var firebaseUid = FirebaseUid;
        if (string.IsNullOrEmpty(firebaseUid))
            return;

        var user = await _userRepository.GetByFirebaseUidAsync(firebaseUid, cancellationToken);
        _resolvedUserId = user?.Id ?? Guid.Empty;
    }
}
