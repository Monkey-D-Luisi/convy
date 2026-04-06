using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Users.DTOs;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Users.Queries;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetUserProfileQueryHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);

        if (user is null)
            return Result<UserDto>.Failure(Error.NotFound("User not found."));

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.CreatedAt));
    }
}
