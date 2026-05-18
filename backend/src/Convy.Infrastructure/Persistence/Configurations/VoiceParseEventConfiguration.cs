using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class VoiceParseEventConfiguration : IEntityTypeConfiguration<VoiceParseEvent>
{
    public void Configure(EntityTypeBuilder<VoiceParseEvent> builder)
    {
        builder.ToTable("voice_parse_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.HouseholdId).HasColumnName("household_id").IsRequired();
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(e => e.AudioSizeBytes).HasColumnName("audio_size_bytes");
        builder.Property(e => e.AudioDurationSeconds).HasColumnName("audio_duration_seconds");
        builder.Property(e => e.ParsedItemsCount).HasColumnName("parsed_items_count").IsRequired();
        builder.Property(e => e.InputTokens).HasColumnName("input_tokens");
        builder.Property(e => e.OutputTokens).HasColumnName("output_tokens");
        builder.Property(e => e.CachedTokens).HasColumnName("cached_tokens");
        builder.Property(e => e.ReasoningTokens).HasColumnName("reasoning_tokens");
        builder.Property(e => e.EstimatedCostMicros).HasColumnName("estimated_cost_micros");
        builder.Property(e => e.LatencyMs).HasColumnName("latency_ms").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_voice_parse_events_created_at");
        builder.HasIndex(e => new { e.HouseholdId, e.CreatedAt }).HasDatabaseName("ix_voice_parse_events_household_id_created_at");
        builder.HasIndex(e => new { e.Status, e.CreatedAt }).HasDatabaseName("ix_voice_parse_events_status_created_at");
    }
}
