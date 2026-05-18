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
        services.AddDbContext<ConvyDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

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
        services.AddScoped<IHouseholdNotificationService, HouseholdNotificationService>();
        services.AddScoped<IFirebaseMessagingClient, FirebaseMessagingClient>();
        services.AddScoped<IPushNotificationTextProvider, PushNotificationTextProvider>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IActivityLogger, ActivityLogger>();
        services.AddScoped<IAdminMetricsReader, AdminMetricsReader>();
        services.AddScoped<IOpenAiVoiceCostEstimator, OpenAiVoiceCostEstimator>();

        services.AddSingleton<PushNotificationBatcher>(sp =>
            new PushNotificationBatcher(
                sp.GetRequiredService<IServiceScopeFactory>(),
                configuration,
                sp.GetRequiredService<ILogger<PushNotificationBatcher>>()));
        services.AddSingleton<IPushNotificationBatcher>(sp => sp.GetRequiredService<PushNotificationBatcher>());
        services.AddHostedService(sp => sp.GetRequiredService<PushNotificationBatcher>());

        services.AddHostedService<RecurringItemService>();

        AddOpenAiServices(services, configuration);

        return services;
    }

    private static void AddOpenAiServices(IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
                     ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddScoped<IAiVoiceParsingService, NoOpVoiceParsingService>();
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
        services.AddScoped<IAiVoiceParsingService, OpenAiVoiceParsingService>();
    }
}
