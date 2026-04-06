using Convy.Application.Common.Models;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Devices.Commands;

public class UnregisterDeviceCommandHandler : IRequestHandler<UnregisterDeviceCommand, Result>
{
    private readonly IDeviceTokenRepository _deviceTokenRepository;

    public UnregisterDeviceCommandHandler(IDeviceTokenRepository deviceTokenRepository)
    {
        _deviceTokenRepository = deviceTokenRepository;
    }

    public async Task<Result> Handle(UnregisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _deviceTokenRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (existing is null)
            return Result.Success();

        _deviceTokenRepository.Remove(existing);
        await _deviceTokenRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
