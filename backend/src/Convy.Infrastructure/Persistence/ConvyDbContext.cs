using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Convy.Infrastructure.Persistence;

public class ConvyDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<HouseholdMembership> HouseholdMemberships => Set<HouseholdMembership>();
    public DbSet<Invite> Invites => Set<Invite>();
    public DbSet<HouseholdList> HouseholdLists => Set<HouseholdList>();
    public DbSet<ListItem> ListItems => Set<ListItem>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<NotificationPreferences> NotificationPreferences => Set<NotificationPreferences>();
    public DbSet<VoiceParseEvent> VoiceParseEvents => Set<VoiceParseEvent>();
    public DbSet<BackupRun> BackupRuns => Set<BackupRun>();
    public DbSet<AiUsageEvent> AiUsageEvents => Set<AiUsageEvent>();
    public DbSet<McpOAuthAuthorizationCode> McpOAuthAuthorizationCodes => Set<McpOAuthAuthorizationCode>();
    public DbSet<McpOAuthRefreshToken> McpOAuthRefreshTokens => Set<McpOAuthRefreshToken>();
    public DbSet<McpOAuthConsent> McpOAuthConsents => Set<McpOAuthConsent>();
    public DbSet<McpToolInvocation> McpToolInvocations => Set<McpToolInvocation>();
    public DbSet<McpIdempotencyRecord> McpIdempotencyRecords => Set<McpIdempotencyRecord>();
    public DbSet<SystemMetricSnapshot> SystemMetricSnapshots => Set<SystemMetricSnapshot>();

    public ConvyDbContext(DbContextOptions<ConvyDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConvyDbContext).Assembly);
    }
}
