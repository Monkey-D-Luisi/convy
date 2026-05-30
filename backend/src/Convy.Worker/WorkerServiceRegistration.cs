using Convy.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Convy.Worker;

public static class WorkerServiceRegistration
{
    public static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddWorkerInfrastructure(configuration);
        return services;
    }
}
