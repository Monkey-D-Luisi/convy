using Microsoft.AspNetCore.SignalR;

namespace Convy.API.Services;

public sealed class CurrentUserHubFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocation,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var currentUser = invocation.ServiceProvider.GetRequiredService<CurrentUserService>();
        await currentUser.ResolveAsync(invocation.Context.User, invocation.Context.ConnectionAborted);
        return await next(invocation);
    }
}
