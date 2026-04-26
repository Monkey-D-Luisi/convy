using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Devices.Commands;

public class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, Result>
{
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly ICurrentUserService _currentUser;

    public RegisterDeviceCommandHandler(IDeviceTokenRepository deviceTokenRepository, ICurrentUserService currentUser)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _deviceTokenRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (existing is not null)
        {
            if (existing.UserId != _currentUser.UserId)
            {
                existing.ReassignTo(_currentUser.UserId, request.Platform, request.Locale);
            }
            else
            {
                existing.UpdateLocale(request.Locale);
            }

            await _deviceTokenRepository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var deviceToken = new DeviceToken(_currentUser.UserId, request.Token, request.Platform, request.Locale);
        await _deviceTokenRepository.AddAsync(deviceToken, cancellationToken);
        await _deviceTokenRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
