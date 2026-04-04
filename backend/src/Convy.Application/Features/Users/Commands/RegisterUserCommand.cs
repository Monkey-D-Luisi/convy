using Convy.Application.Common.Models;
using Convy.Application.Features.Users.DTOs;
using MediatR;

namespace Convy.Application.Features.Users.Commands;

public record RegisterUserCommand(string FirebaseUid, string DisplayName, string Email) : IRequest<Result<UserDto>>;
