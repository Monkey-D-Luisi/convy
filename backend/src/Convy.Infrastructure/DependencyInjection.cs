using Convy.Application.Common.Interfaces;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Persistence;
using Convy.Infrastructure.Repositories;
using Convy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Responses;

namespace Convy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddCoreInfrastructure(services, configuration);
        AddApiNotificationServices(services, configuration);
        AddOpenAiServices(services, configuration);

        return services;
    }

    public static IServiceCollection AddWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddCoreInfrastructure(services, configuration);

        services.AddHostedService<RecurringItemService>();
        services.AddHostedService<TaskReminderService>();
        services.AddHostedService<SystemMetricSnapshotHostedService>();

        return services;
    }

    private static void AddCoreInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ConvyDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(2),
                    errorCodesToAdd: ["40001", "40P01"])));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHouseholdRepository, HouseholdRepository>();
        services.AddScoped<IInviteRepository, InviteRepository>();
        services.AddScoped<IHouseholdListRepository, HouseholdListRepository>();
        services.AddScoped<IListItemRepository, ListItemRepository>();
        services.AddScoped<ITaskItemRepository, TaskItemRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
        services.AddScoped<INotificationPreferencesRepository, NotificationPreferencesRepository>();
        services.AddScoped<IVoiceParseEventRepository, VoiceParseEventRepository>();
        services.AddScoped<IBackupRunRepository, BackupRunRepository>();
        services.AddScoped<IFirebaseMessagingClient, FirebaseMessagingClient>();
        services.AddScoped<IPushNotificationTextProvider, PushNotificationTextProvider>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<ITaskReminderProcessingLock, PostgresTaskReminderProcessingLock>();
        services.AddScoped<IActivityLogger, ActivityLogger>();
        services.AddScoped<IAdminMetricsReader, AdminMetricsReader>();
        services.AddScoped<IAdminBackupFileService, AdminBackupFileService>();
        services.AddScoped<IOpenAiVoiceCostEstimator, OpenAiVoiceCostEstimator>();
        services.AddScoped<IAiUsageRecorder, AiUsageRecorder>();
        services.AddScoped<ISystemMetricSource, SystemMetricSource>();
        services.AddScoped<SystemMetricSnapshotRecorder>();
    }

    private static void AddApiNotificationServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IHouseholdNotificationService, HouseholdNotificationService>();
        services.AddSingleton<PushNotificationBatcher>(sp =>
            new PushNotificationBatcher(
                sp.GetRequiredService<IServiceScopeFactory>(),
                configuration,
                sp.GetRequiredService<ILogger<PushNotificationBatcher>>()));
        services.AddSingleton<IPushNotificationBatcher>(sp => sp.GetRequiredService<PushNotificationBatcher>());
        services.AddHostedService(sp => sp.GetRequiredService<PushNotificationBatcher>());
    }

    private static void AddOpenAiServices(IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
                     ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var voiceParsingEnabled = configuration.GetValue<bool>("Features:VoiceParsingEnabled");
        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"]
                              ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? "Production";

        if (!voiceParsingEnabled)
        {
            services.AddScoped<IAiVoiceParsingService, NoOpVoiceParsingService>();
            services.AddScoped<ITaskVoiceParsingService, NoOpTaskVoiceParsingService>();
            return;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (!string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("OpenAI API key is required when voice parsing is enabled outside Development.");
            }

            services.AddScoped<IAiVoiceParsingService, NoOpVoiceParsingService>();
            services.AddScoped<ITaskVoiceParsingService, NoOpTaskVoiceParsingService>();
            return;
        }

        var transcriptionModel = configuration["OpenAI:TranscriptionModel"] ?? "gpt-4o-mini-transcribe";
        var parsingModel = configuration["OpenAI:ParsingModel"] ?? "gpt-5.4-nano";
        var openAiOptions = new OpenAiVoiceParsingOptions(transcriptionModel, parsingModel);

        var openAiClient = new OpenAIClient(apiKey);

        services.AddSingleton(openAiClient.GetAudioClient(transcriptionModel));
        services.AddSingleton(openAiClient.GetResponsesClient());
        services.AddSingleton(openAiOptions);
        services.AddScoped<IOpenAiVoiceTranscriptionClient, OpenAiVoiceTranscriptionClient>();
        services.AddScoped<IOpenAiResponsesClient, OpenAiResponsesClient>();
        services.AddScoped<IOpenAiVoiceItemParser, OpenAiVoiceItemParser>();
        services.AddScoped<IOpenAiVoiceTaskParser, OpenAiVoiceTaskParser>();
        services.AddScoped<IAiVoiceParsingService, OpenAiVoiceParsingService>();
        services.AddScoped<ITaskVoiceParsingService, OpenAiTaskVoiceParsingService>();
    }
}
