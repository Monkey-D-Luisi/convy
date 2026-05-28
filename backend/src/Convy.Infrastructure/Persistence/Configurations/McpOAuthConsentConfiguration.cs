using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class McpOAuthConsentConfiguration : IEntityTypeConfiguration<McpOAuthConsent>
{
    public void Configure(EntityTypeBuilder<McpOAuthConsent> builder)
    {
        builder.ToTable("mcp_oauth_consents");

        builder.HasKey(consent => consent.Id);
        builder.Property(consent => consent.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(consent => consent.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(consent => consent.ClientId).HasColumnName("client_id").HasMaxLength(500).IsRequired();
        builder.Property(consent => consent.Resource).HasColumnName("resource").HasMaxLength(500).IsRequired();
        builder.Property(consent => consent.Scopes).HasColumnName("scopes").HasMaxLength(500).IsRequired();
        builder.Property(consent => consent.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(consent => consent.RevokedAt).HasColumnName("revoked_at");

        builder.HasIndex(consent => new { consent.UserId, consent.ClientId, consent.Resource })
            .HasDatabaseName("ix_mcp_oauth_consents_user_id_client_id_resource");
        builder.HasIndex(consent => consent.RevokedAt).HasDatabaseName("ix_mcp_oauth_consents_revoked_at");
    }
}
