using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Devices.Commands;

public class UnregisterDeviceCommandHandler : IRequestHandler<UnregisterDeviceCommand, Result>
{
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly ICurrentUserService _currentUser;

    public UnregisterDeviceCommandHandler(
        IDeviceTokenRepository deviceTokenRepository,
        ICurrentUserService currentUser)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UnregisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _deviceTokenRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (existing is null)
            return Result.Success();

        // Silently succeed without deleting if the token belongs to another user.
        // This avoids leaking information about token ownership.
        if (existing.UserId != _currentUser.UserId)
            return Result.Success();

        _deviceTokenRepository.Remove(existing);
        await _deviceTokenRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
