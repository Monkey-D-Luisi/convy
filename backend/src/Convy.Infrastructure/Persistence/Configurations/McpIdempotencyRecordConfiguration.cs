using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class McpIdempotencyRecordConfiguration : IEntityTypeConfiguration<McpIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<McpIdempotencyRecord> builder)
    {
        builder.ToTable("mcp_idempotency_records");

        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(record => record.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(record => record.ClientId).HasColumnName("client_id").HasMaxLength(500).IsRequired();
        builder.Property(record => record.KeyHash).HasColumnName("key_hash").HasMaxLength(128).IsRequired();
        builder.Property(record => record.ActionName).HasColumnName("action_name").HasMaxLength(100).IsRequired();
        builder.Property(record => record.RequestHash).HasColumnName("request_hash").HasMaxLength(128).IsRequired();
        builder.Property(record => record.StatusCode).HasColumnName("status_code").IsRequired();
        builder.Property(record => record.Location).HasColumnName("location").HasMaxLength(500);
        builder.Property(record => record.ResponseJson).HasColumnName("response_json").HasColumnType("jsonb");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(record => record.ExpiresAt).HasColumnName("expires_at").IsRequired();

        builder.HasIndex(record => new { record.UserId, record.ClientId, record.KeyHash })
            .IsUnique()
            .HasDatabaseName("ix_mcp_idempotency_records_user_client_key");
        builder.HasIndex(record => record.ExpiresAt).HasDatabaseName("ix_mcp_idempotency_records_expires_at");
    }
}
