using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("task_items");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Note)
            .HasColumnName("note")
            .HasMaxLength(500);

        builder.Property(t => t.ListId)
            .HasColumnName("list_id")
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.IsCompleted)
            .HasColumnName("is_completed")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.CompletedBy)
            .HasColumnName("completed_by");

        builder.Property(t => t.CompletedAt)
            .HasColumnName("completed_at");

        builder.HasIndex(t => t.ListId)
            .HasDatabaseName("ix_task_items_list_id");

        builder.HasIndex(t => new { t.ListId, t.IsCompleted })
            .HasDatabaseName("ix_task_items_list_id_is_completed");
    }
}
