using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class ListItemConfiguration : IEntityTypeConfiguration<ListItem>
{
    public void Configure(EntityTypeBuilder<ListItem> builder)
    {
        builder.ToTable("list_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Quantity)
            .HasColumnName("quantity");

        builder.Property(i => i.Unit)
            .HasColumnName("unit")
            .HasMaxLength(50);

        builder.Property(i => i.Note)
            .HasColumnName("note")
            .HasMaxLength(500);

        builder.Property(i => i.ListId)
            .HasColumnName("list_id")
            .IsRequired();

        builder.Property(i => i.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(i => i.IsCompleted)
            .HasColumnName("is_completed")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.CompletedBy)
            .HasColumnName("completed_by");

        builder.Property(i => i.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(i => i.RecurrenceFrequency)
            .HasColumnName("recurrence_frequency")
            .HasConversion<int?>();

        builder.Property(i => i.RecurrenceInterval)
            .HasColumnName("recurrence_interval");

        builder.Property(i => i.NextDueDate)
            .HasColumnName("next_due_date");

        builder.HasIndex(i => i.ListId)
            .HasDatabaseName("ix_list_items_list_id");

        builder.HasIndex(i => new { i.ListId, i.IsCompleted })
            .HasDatabaseName("ix_list_items_list_id_is_completed");

        builder.HasIndex(i => i.NextDueDate)
            .HasDatabaseName("ix_list_items_next_due_date")
            .HasFilter("\"next_due_date\" IS NOT NULL");
    }
}
