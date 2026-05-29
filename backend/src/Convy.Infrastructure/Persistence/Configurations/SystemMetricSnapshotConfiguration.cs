using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class SystemMetricSnapshotConfiguration : IEntityTypeConfiguration<SystemMetricSnapshot>
{
    public void Configure(EntityTypeBuilder<SystemMetricSnapshot> builder)
    {
        builder.ToTable("system_metric_snapshots");

        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(snapshot => snapshot.CapturedAt).HasColumnName("captured_at").IsRequired();
        builder.Property(snapshot => snapshot.DiskFreeBytes).HasColumnName("disk_free_bytes");
        builder.Property(snapshot => snapshot.DiskTotalBytes).HasColumnName("disk_total_bytes");
        builder.Property(snapshot => snapshot.MemoryAvailableBytes).HasColumnName("memory_available_bytes");
        builder.Property(snapshot => snapshot.MemoryTotalBytes).HasColumnName("memory_total_bytes");
        builder.Property(snapshot => snapshot.LoadAverage1m).HasColumnName("load_average_1m");
        builder.Property(snapshot => snapshot.UptimeSeconds).HasColumnName("uptime_seconds");
        builder.Property(snapshot => snapshot.PostgresDataSizeBytes).HasColumnName("postgres_data_size_bytes");

        builder.HasIndex(snapshot => snapshot.CapturedAt)
            .HasDatabaseName("ix_system_metric_snapshots_captured_at");
    }
}
