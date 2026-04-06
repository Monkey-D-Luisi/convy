using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Devices.Commands;

public record UnregisterDeviceCommand(string Token) : IRequest<Result>;
