using Convy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Convy.Infrastructure.Persistence.Configurations;

public class McpToolInvocationConfiguration : IEntityTypeConfiguration<McpToolInvocation>
{
    public void Configure(EntityTypeBuilder<McpToolInvocation> builder)
    {
        builder.ToTable("mcp_tool_invocations");

        builder.HasKey(invocation => invocation.Id);
        builder.Property(invocation => invocation.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(invocation => invocation.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(invocation => invocation.HouseholdId).HasColumnName("household_id");
        builder.Property(invocation => invocation.ToolName).HasColumnName("tool_name").HasMaxLength(100).IsRequired();
        builder.Property(invocation => invocation.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(invocation => invocation.LatencyMs).HasColumnName("latency_ms").IsRequired();
        builder.Property(invocation => invocation.ErrorType).HasColumnName("error_type").HasMaxLength(100);
        builder.Property(invocation => invocation.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(invocation => invocation.UserId).HasDatabaseName("ix_mcp_tool_invocations_user_id");
        builder.HasIndex(invocation => invocation.HouseholdId).HasDatabaseName("ix_mcp_tool_invocations_household_id");
        builder.HasIndex(invocation => invocation.ToolName).HasDatabaseName("ix_mcp_tool_invocations_tool_name");
        builder.HasIndex(invocation => invocation.CreatedAt).HasDatabaseName("ix_mcp_tool_invocations_created_at");
    }
}
