using Convy.Application.Common.Interfaces;
using Convy.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Convy.Infrastructure.Tests;

public class DependencyInjectionWorkerBoundaryTests
{
    [Fact]
    public void AddInfrastructure_DoesNotRegisterScheduledWorkerHostedServices()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(CreateConfiguration());

        var hostedTypes = GetHostedServiceImplementationTypes(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(PushNotificationBatcher));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IPushNotificationBatcher));
        hostedTypes.Should().NotContain(typeof(RecurringItemService));
        hostedTypes.Should().NotContain(typeof(TaskReminderService));
        hostedTypes.Should().NotContain(typeof(SystemMetricSnapshotHostedService));
    }

    [Fact]
    public void AddWorkerInfrastructure_RegistersScheduledWorkerHostedServices()
    {
        var services = new ServiceCollection();

        services.AddWorkerInfrastructure(CreateConfiguration());

        var hostedTypes = GetHostedServiceImplementationTypes(services);
        hostedTypes.Should().Contain(typeof(RecurringItemService));
        hostedTypes.Should().Contain(typeof(TaskReminderService));
        hostedTypes.Should().Contain(typeof(SystemMetricSnapshotHostedService));
        hostedTypes.Should().NotContain(typeof(PushNotificationBatcher));
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
