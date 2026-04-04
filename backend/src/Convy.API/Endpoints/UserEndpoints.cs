using Convy.Application.Common.Models;
using Convy.Application.Features.Users.Commands;
using Convy.Application.Features.Users.DTOs;
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
            IMediator mediator) =>
        {
            var command = new RegisterUserCommand(request.FirebaseUid, request.DisplayName, request.Email);
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/users/{result.Value!.Id}", result.Value)
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

public record RegisterUserRequest(string FirebaseUid, string DisplayName, string Email);
