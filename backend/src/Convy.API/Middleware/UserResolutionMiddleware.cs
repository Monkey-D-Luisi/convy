using Convy.API.Services;

namespace Convy.API.Middleware;

public class UserResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public UserResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CurrentUserService currentUserService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await currentUserService.ResolveAsync(context.RequestAborted);
        }

        await _next(context);
    }
}
