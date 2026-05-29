using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedTitlesAndMcpClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_title",
                table: "task_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_id",
                table: "mcp_tool_invocations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_title",
                table: "list_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_items_list_id_normalized_title_is_completed",
                table: "task_items",
                columns: new[] { "list_id", "normalized_title", "is_completed" });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_tool_invocations_client_id",
                table: "mcp_tool_invocations",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_list_items_list_id_normalized_title_is_completed",
                table: "list_items",
                columns: new[] { "list_id", "normalized_title", "is_completed" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_task_items_list_id_normalized_title_is_completed",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "ix_mcp_tool_invocations_client_id",
                table: "mcp_tool_invocations");

            migrationBuilder.DropIndex(
                name: "ix_list_items_list_id_normalized_title_is_completed",
                table: "list_items");

            migrationBuilder.DropColumn(
                name: "normalized_title",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "client_id",
                table: "mcp_tool_invocations");

            migrationBuilder.DropColumn(
                name: "normalized_title",
                table: "list_items");
        }
    }
}
