using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringItemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "next_due_date",
                table: "list_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "recurrence_frequency",
                table: "list_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "recurrence_interval",
                table: "list_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "device_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_tokens", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_list_items_next_due_date",
                table: "list_items",
                column: "next_due_date",
                filter: "\"next_due_date\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_device_tokens_token",
                table: "device_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_tokens_user_id",
                table: "device_tokens",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_tokens");

            migrationBuilder.DropIndex(
                name: "ix_list_items_next_due_date",
                table: "list_items");

            migrationBuilder.DropColumn(
                name: "next_due_date",
                table: "list_items");

            migrationBuilder.DropColumn(
                name: "recurrence_frequency",
                table: "list_items");

            migrationBuilder.DropColumn(
                name: "recurrence_interval",
                table: "list_items");
        }
    }
}
