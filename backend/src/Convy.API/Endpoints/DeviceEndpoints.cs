using Convy.Application.Common.Models;
using Convy.Application.Features.Devices.Commands;
using MediatR;

namespace Convy.API.Endpoints;

public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/devices")
            .WithTags("Devices")
            .RequireAuthorization();

        group.MapPost("/register", async (RegisterDeviceRequest request, IMediator mediator) =>
        {
            var command = new RegisterDeviceCommand(request.Token, request.Platform);
            var result = await mediator.Send(command);
            return result.IsSuccess ? Results.Ok() : MapError(result.Error!);
        });

        group.MapDelete("/{token}", async (string token, IMediator mediator) =>
        {
            var command = new UnregisterDeviceCommand(token);
            var result = await mediator.Send(command);
            return result.IsSuccess ? Results.NoContent() : MapError(result.Error!);
        });
    }

    private static IResult MapError(Error error) => error.Code switch
    {
        "NotFound" => Results.NotFound(error),
        "Validation" => Results.BadRequest(error),
        _ => Results.Problem(error.Message, statusCode: 500)
    };
}

public record RegisterDeviceRequest(string Token, string Platform);
