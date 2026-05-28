using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMcpOAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mcp_oauth_authorization_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    redirect_uri = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    resource = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    scopes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    code_challenge = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    code_challenge_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcp_oauth_authorization_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mcp_oauth_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    resource = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    scopes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcp_oauth_consents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mcp_oauth_refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    resource = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    scopes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcp_oauth_refresh_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mcp_tool_invocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tool_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    latency_ms = table.Column<long>(type: "bigint", nullable: false),
                    error_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcp_tool_invocations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_oauth_authorization_codes_code_hash",
                table: "mcp_oauth_authorization_codes",
                column: "code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mcp_oauth_authorization_codes_expires_at",
                table: "mcp_oauth_authorization_codes",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_oauth_authorization_codes_user_id",
                table: "mcp_oauth_authorization_codes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_oauth_consents_revoked_at",
                table: "mcp_oauth_consents",
                column: "revoked_at");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_oauth_consents_user_id_client_id_resource",
                table: "mcp_oauth_consents",
                columns: new[] { "user_id", "client_id", "resource" });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_oauth_refresh_tokens_expires_at",
                table: "mcp_oauth_refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_oauth_refresh_tokens_token_hash",
                table: "mcp_oauth_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mcp_oauth_refresh_tokens_user_id",
                table: "mcp_oauth_refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_tool_invocations_created_at",
                table: "mcp_tool_invocations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_tool_invocations_household_id",
                table: "mcp_tool_invocations",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_tool_invocations_tool_name",
                table: "mcp_tool_invocations",
                column: "tool_name");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_tool_invocations_user_id",
                table: "mcp_tool_invocations",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mcp_oauth_authorization_codes");

            migrationBuilder.DropTable(
                name: "mcp_oauth_consents");

            migrationBuilder.DropTable(
                name: "mcp_oauth_refresh_tokens");

            migrationBuilder.DropTable(
                name: "mcp_tool_invocations");
        }
    }
}
