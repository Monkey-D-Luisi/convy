using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ToTable("device_tokens");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(d => d.Token)
            .HasColumnName("token")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.Platform)
            .HasColumnName("platform")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(d => d.Token).IsUnique().HasDatabaseName("ix_device_tokens_token");
        builder.HasIndex(d => d.UserId).HasDatabaseName("ix_device_tokens_user_id");
    }
}
