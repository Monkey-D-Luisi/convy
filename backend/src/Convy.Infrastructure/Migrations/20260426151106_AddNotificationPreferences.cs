using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "locale",
                table: "device_tokens",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    items_added = table.Column<bool>(type: "boolean", nullable: false),
                    tasks_added = table.Column<bool>(type: "boolean", nullable: false),
                    items_completed = table.Column<bool>(type: "boolean", nullable: false),
                    tasks_completed = table.Column<bool>(type: "boolean", nullable: false),
                    item_task_changes = table.Column<bool>(type: "boolean", nullable: false),
                    list_changes = table.Column<bool>(type: "boolean", nullable: false),
                    member_changes = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_preferences_user_id",
                table: "notification_preferences",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "locale",
                table: "device_tokens");
        }
    }
}
