using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(u => u.FirebaseUid)
            .HasColumnName("firebase_uid")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(u => u.FirebaseUid).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
