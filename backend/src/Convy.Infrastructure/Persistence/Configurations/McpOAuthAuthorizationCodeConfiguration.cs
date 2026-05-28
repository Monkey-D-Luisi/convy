using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class McpOAuthAuthorizationCodeConfiguration : IEntityTypeConfiguration<McpOAuthAuthorizationCode>
{
    public void Configure(EntityTypeBuilder<McpOAuthAuthorizationCode> builder)
    {
        builder.ToTable("mcp_oauth_authorization_codes");

        builder.HasKey(code => code.Id);
        builder.Property(code => code.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(code => code.CodeHash).HasColumnName("code_hash").HasMaxLength(128).IsRequired();
        builder.Property(code => code.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(code => code.ClientId).HasColumnName("client_id").HasMaxLength(500).IsRequired();
        builder.Property(code => code.RedirectUri).HasColumnName("redirect_uri").HasMaxLength(1000).IsRequired();
        builder.Property(code => code.Resource).HasColumnName("resource").HasMaxLength(500).IsRequired();
        builder.Property(code => code.Scopes).HasColumnName("scopes").HasMaxLength(500).IsRequired();
        builder.Property(code => code.CodeChallenge).HasColumnName("code_challenge").HasMaxLength(256).IsRequired();
        builder.Property(code => code.CodeChallengeMethod).HasColumnName("code_challenge_method").HasMaxLength(20).IsRequired();
        builder.Property(code => code.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(code => code.UsedAt).HasColumnName("used_at");
        builder.Property(code => code.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(code => code.CodeHash).IsUnique().HasDatabaseName("ix_mcp_oauth_authorization_codes_code_hash");
        builder.HasIndex(code => code.UserId).HasDatabaseName("ix_mcp_oauth_authorization_codes_user_id");
        builder.HasIndex(code => code.ExpiresAt).HasDatabaseName("ix_mcp_oauth_authorization_codes_expires_at");
    }
}
