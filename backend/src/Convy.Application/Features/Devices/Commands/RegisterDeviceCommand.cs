using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Devices.Commands;

public record RegisterDeviceCommand(string Token, string Platform) : IRequest<Result>;
