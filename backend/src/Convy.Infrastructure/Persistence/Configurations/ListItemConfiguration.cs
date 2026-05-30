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
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.NormalizedTitle)
            .HasColumnName("normalized_title")
            .HasMaxLength(200);

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

        builder.Property(i => i.ReturnedToPendingBy)
            .HasColumnName("returned_to_pending_by");

        builder.Property(i => i.ReturnedToPendingAt)
            .HasColumnName("returned_to_pending_at");

        builder.Property(i => i.RecurrenceFrequency)
            .HasColumnName("recurrence_frequency")
            .HasConversion<int?>();

        builder.Property(i => i.RecurrenceInterval)
            .HasColumnName("recurrence_interval");

        builder.Property(i => i.NextDueDate)
            .HasColumnName("next_due_date");

        builder.Property(i => i.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(Domain.ValueObjects.ItemCreationSource.Manual);

        builder.HasIndex(i => i.ListId)
            .HasDatabaseName("ix_list_items_list_id");

        builder.HasIndex(i => new { i.ListId, i.IsCompleted })
            .HasDatabaseName("ix_list_items_list_id_is_completed");

        builder.HasIndex(i => new { i.ListId, i.IsCompleted, i.CreatedAt })
            .HasDatabaseName("ix_list_items_list_id_is_completed_created_at");

        builder.HasIndex(i => new { i.ListId, i.IsCompleted, i.CompletedAt })
            .HasDatabaseName("ix_list_items_list_id_is_completed_completed_at");

        builder.HasIndex(i => new { i.ListId, i.IsCompleted, i.ReturnedToPendingAt })
            .HasDatabaseName("ix_list_items_list_id_is_completed_returned_at");

        builder.HasIndex(i => new { i.ListId, i.NormalizedTitle, i.IsCompleted })
            .HasDatabaseName("ix_list_items_list_id_normalized_title_is_completed");

        builder.HasIndex(i => i.NextDueDate)
            .HasDatabaseName("ix_list_items_next_due_date")
            .HasFilter("\"next_due_date\" IS NOT NULL");

        builder.HasIndex(i => new { i.Source, i.CreatedAt })
            .HasDatabaseName("ix_list_items_source_created_at");

        builder.HasIndex(i => i.CreatedBy)
            .HasDatabaseName("ix_list_items_created_by");

        builder.HasIndex(i => i.CompletedBy)
            .HasDatabaseName("ix_list_items_completed_by");

        builder.HasIndex(i => i.ReturnedToPendingBy)
            .HasDatabaseName("ix_list_items_returned_to_pending_by");

        builder.HasOne<HouseholdList>()
            .WithMany()
            .HasForeignKey(i => i.ListId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_list_items_household_lists_list_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_list_items_users_created_by");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.CompletedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_list_items_users_completed_by");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.ReturnedToPendingBy)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_list_items_users_returned_to_pending_by");
    }
}
