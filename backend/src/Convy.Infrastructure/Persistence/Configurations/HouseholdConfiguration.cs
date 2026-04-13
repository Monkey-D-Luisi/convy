using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.ToTable("households");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(h => h.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasMany(h => h.Memberships)
            .WithOne()
            .HasForeignKey(m => m.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
