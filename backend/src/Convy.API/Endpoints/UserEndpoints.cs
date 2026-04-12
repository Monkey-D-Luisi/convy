using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Application.Features.Users.Commands;
using Convy.Application.Features.Users.DTOs;
using Convy.Application.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Convy.API.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/users")
            .WithTags("Users");

        group.MapPost("/register", [Authorize] async (
            RegisterUserRequest request,
            IMediator mediator,
            ICurrentUserService currentUser) =>
        {
            var command = new RegisterUserCommand(currentUser.FirebaseUid, request.DisplayName, request.Email);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/users/{result.Value!.Id}", result.Value)
                : MapError(result.Error!);
        });

        group.MapGet("/me", [Authorize] async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetUserProfileQuery());

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error!);
        });
    }

    private static IResult MapError(Error error) => error.Code switch
    {
        "NotFound" => Results.NotFound(error),
        "Validation" => Results.BadRequest(error),
        "Conflict" => Results.Conflict(error),
        "Forbidden" => Results.Forbid(),
        _ => Results.Problem(error.Message, statusCode: 500)
    };
}

public record RegisterUserRequest(string DisplayName, string Email);
