using Convy.Application.Common.Models;
using Convy.Application.Features.Users.DTOs;
using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using MediatR;

namespace Convy.Application.Features.Users.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public RegisterUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByFirebaseUidAsync(request.FirebaseUid, cancellationToken);

        if (existing is not null)
        {
            return Result<UserDto>.Success(new UserDto(
                existing.Id,
                existing.DisplayName,
                existing.Email,
                existing.CreatedAt));
        }

        // Firebase UID not found — check if email already exists (Firebase account was recreated)
        var existingByEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (existingByEmail is not null)
        {
            existingByEmail.UpdateFirebaseUid(request.FirebaseUid);
            await _userRepository.SaveChangesAsync(cancellationToken);

            return Result<UserDto>.Success(new UserDto(
                existingByEmail.Id,
                existingByEmail.DisplayName,
                existingByEmail.Email,
                existingByEmail.CreatedAt));
        }

        var user = new User(request.FirebaseUid, request.DisplayName, request.Email);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.CreatedAt));
    }
}
