using Convy.Application.Common.Interfaces;
using Convy.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Convy.Worker.Tests;

public class WorkerServiceRegistrationTests
{
    [Fact]
    public void ConfigureServices_RegistersScheduledWorkerJobs()
    {
        var services = new ServiceCollection();

        WorkerServiceRegistration.ConfigureServices(services, CreateConfiguration());

        var hostedTypes = GetHostedServiceImplementationTypes(services);
        hostedTypes.Should().Contain(typeof(RecurringItemService));
        hostedTypes.Should().Contain(typeof(TaskReminderService));
        hostedTypes.Should().Contain(typeof(SystemMetricSnapshotHostedService));
    }

    [Fact]
    public void ConfigureServices_DoesNotRegisterApiPushBatcher()
    {
        var services = new ServiceCollection();

        WorkerServiceRegistration.ConfigureServices(services, CreateConfiguration());

        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(PushNotificationBatcher));
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(IPushNotificationBatcher));
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=convy;Username=convy;Password=convy",
                ["Features:VoiceParsingEnabled"] = "false"
            })
            .Build();

    private static IReadOnlyCollection<Type> GetHostedServiceImplementationTypes(IServiceCollection services) =>
        services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .Select(descriptor => descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType())
            .Where(type => type is not null)
            .Cast<Type>()
            .ToList();
}
