using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class InviteConfiguration : IEntityTypeConfiguration<Invite>
{
    public void Configure(EntityTypeBuilder<Invite> builder)
    {
        builder.ToTable("invites");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.HouseholdId)
            .HasColumnName("household_id")
            .IsRequired();

        builder.Property(i => i.Code)
            .HasColumnName("code")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(i => i.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(i => i.UsedAt)
            .HasColumnName("used_at");

        builder.Property(i => i.UsedBy)
            .HasColumnName("used_by");

        builder.Property(i => i.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(i => i.Code).IsUnique();
        builder.HasIndex(i => i.HouseholdId);
    }
}
