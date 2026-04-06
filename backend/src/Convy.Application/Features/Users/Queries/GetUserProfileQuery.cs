using Convy.Application.Common.Models;
using Convy.Application.Features.Users.DTOs;
using MediatR;

namespace Convy.Application.Features.Users.Queries;

public record GetUserProfileQuery : IRequest<Result<UserDto>>;
