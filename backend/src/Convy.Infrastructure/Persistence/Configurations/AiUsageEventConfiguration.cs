using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class AiUsageEventConfiguration : IEntityTypeConfiguration<AiUsageEvent>
{
    public void Configure(EntityTypeBuilder<AiUsageEvent> builder)
    {
        builder.ToTable("ai_usage_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.HouseholdId).HasColumnName("household_id");

        builder.Property(e => e.Feature)
            .HasColumnName("feature")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Operation)
            .HasColumnName("operation")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Model)
            .HasColumnName("model")
            .HasMaxLength(100);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ErrorType)
            .HasColumnName("error_type")
            .HasMaxLength(100);

        builder.Property(e => e.LatencyMs)
            .HasColumnName("latency_ms")
            .IsRequired();

        builder.Property(e => e.InputTokens).HasColumnName("input_tokens");
        builder.Property(e => e.OutputTokens).HasColumnName("output_tokens");
        builder.Property(e => e.CachedTokens).HasColumnName("cached_tokens");
        builder.Property(e => e.ReasoningTokens).HasColumnName("reasoning_tokens");
        builder.Property(e => e.AudioTokens).HasColumnName("audio_tokens");
        builder.Property(e => e.TextTokens).HasColumnName("text_tokens");
        builder.Property(e => e.AudioDurationSeconds).HasColumnName("audio_duration_seconds");
        builder.Property(e => e.EstimatedCostMicros).HasColumnName("estimated_cost_micros");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_ai_usage_events_created_at");

        builder.HasIndex(e => new { e.Feature, e.Operation, e.CreatedAt })
            .HasDatabaseName("ix_ai_usage_events_feature_operation_created_at");

        builder.HasIndex(e => new { e.HouseholdId, e.CreatedAt })
            .HasDatabaseName("ix_ai_usage_events_household_id_created_at");
    }
}
