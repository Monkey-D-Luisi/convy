using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class McpOAuthRefreshTokenConfiguration : IEntityTypeConfiguration<McpOAuthRefreshToken>
{
    public void Configure(EntityTypeBuilder<McpOAuthRefreshToken> builder)
    {
        builder.ToTable("mcp_oauth_refresh_tokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(token => token.ClientId).HasColumnName("client_id").HasMaxLength(500).IsRequired();
        builder.Property(token => token.Resource).HasColumnName("resource").HasMaxLength(500).IsRequired();
        builder.Property(token => token.Scopes).HasColumnName("scopes").HasMaxLength(500).IsRequired();
        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(token => token.RevokedAt).HasColumnName("revoked_at");
        builder.Property(token => token.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(token => token.LastUsedAt).HasColumnName("last_used_at");
        builder.Property(token => token.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(128);

        builder.HasIndex(token => token.TokenHash).IsUnique().HasDatabaseName("ix_mcp_oauth_refresh_tokens_token_hash");
        builder.HasIndex(token => token.UserId).HasDatabaseName("ix_mcp_oauth_refresh_tokens_user_id");
        builder.HasIndex(token => token.ExpiresAt).HasDatabaseName("ix_mcp_oauth_refresh_tokens_expires_at");
    }
}
