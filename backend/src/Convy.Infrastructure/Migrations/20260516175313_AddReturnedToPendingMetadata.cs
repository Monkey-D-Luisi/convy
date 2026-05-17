using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnedToPendingMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "returned_to_pending_at",
                table: "list_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "returned_to_pending_by",
                table: "list_items",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "returned_to_pending_at",
                table: "list_items");

            migrationBuilder.DropColumn(
                name: "returned_to_pending_by",
                table: "list_items");
        }
    }
}
