using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class NotificationPreferencesConfiguration : IEntityTypeConfiguration<NotificationPreferences>
{
    public void Configure(EntityTypeBuilder<NotificationPreferences> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(p => p.ItemsAdded)
            .HasColumnName("items_added")
            .IsRequired();

        builder.Property(p => p.TasksAdded)
            .HasColumnName("tasks_added")
            .IsRequired();

        builder.Property(p => p.ItemsCompleted)
            .HasColumnName("items_completed")
            .IsRequired();

        builder.Property(p => p.TasksCompleted)
            .HasColumnName("tasks_completed")
            .IsRequired();

        builder.Property(p => p.ItemTaskChanges)
            .HasColumnName("item_task_changes")
            .IsRequired();

        builder.Property(p => p.ListChanges)
            .HasColumnName("list_changes")
            .IsRequired();

        builder.Property(p => p.MemberChanges)
            .HasColumnName("member_changes")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasDatabaseName("ix_notification_preferences_user_id");
    }
}
