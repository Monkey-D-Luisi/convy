using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class BackupRunConfiguration : IEntityTypeConfiguration<BackupRun>
{
    public void Configure(EntityTypeBuilder<BackupRun> builder)
    {
        builder.ToTable("backup_runs");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(b => b.BackupType)
            .HasColumnName("backup_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(b => b.FileName).HasColumnName("file_name").HasMaxLength(300);
        builder.Property(b => b.SizeBytes).HasColumnName("size_bytes");
        builder.Property(b => b.Sha256).HasColumnName("sha256").HasMaxLength(64);
        builder.Property(b => b.DurationMs).HasColumnName("duration_ms").IsRequired();
        builder.Property(b => b.VerificationStatus)
            .HasColumnName("verification_status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(b => b.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(b => b.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(b => b.FinishedAt).HasColumnName("finished_at").IsRequired();

        builder.HasIndex(b => b.StartedAt)
            .HasDatabaseName("ix_backup_runs_started_at")
            .IsDescending();
        builder.HasIndex(b => new { b.Status, b.StartedAt })
            .HasDatabaseName("ix_backup_runs_status_started_at")
            .IsDescending(false, true);
    }
}
