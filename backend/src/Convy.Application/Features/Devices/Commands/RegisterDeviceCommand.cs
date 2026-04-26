using Convy.Application.Common.Models;
using MediatR;

namespace Convy.Application.Features.Devices.Commands;

public record RegisterDeviceCommand(string Token, string Platform, string? Locale = null) : IRequest<Result>;
