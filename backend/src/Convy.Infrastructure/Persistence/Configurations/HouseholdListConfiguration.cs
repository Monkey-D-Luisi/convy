using Convy.Domain.Entities;
using Convy.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class HouseholdListConfiguration : IEntityTypeConfiguration<HouseholdList>
{
    public void Configure(EntityTypeBuilder<HouseholdList> builder)
    {
        builder.ToTable("household_lists");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");

        builder.Property(l => l.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.HouseholdId)
            .HasColumnName("household_id")
            .IsRequired();

        builder.Property(l => l.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(l => l.IsArchived)
            .HasColumnName("is_archived")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(l => l.ArchivedAt)
            .HasColumnName("archived_at");

        builder.HasIndex(l => l.HouseholdId)
            .HasDatabaseName("ix_household_lists_household_id");

        builder.HasIndex(l => new { l.HouseholdId, l.IsArchived })
            .HasDatabaseName("ix_household_lists_household_id_is_archived");
    }
}
