using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.HouseholdId)
            .HasColumnName("household_id")
            .IsRequired();

        builder.Property(a => a.EntityType)
            .HasColumnName("entity_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(a => a.ActionType)
            .HasColumnName("action_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.PerformedBy)
            .HasColumnName("performed_by")
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(a => a.Metadata)
            .HasColumnName("metadata")
            .HasMaxLength(2000);

        builder.HasIndex(a => new { a.HouseholdId, a.CreatedAt })
            .HasDatabaseName("ix_activity_logs_household_id_created_at")
            .IsDescending(false, true);
    }
}
