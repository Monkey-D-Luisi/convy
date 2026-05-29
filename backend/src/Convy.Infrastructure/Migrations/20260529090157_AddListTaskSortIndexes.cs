using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListTaskSortIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_task_items_list_id_created_at",
                table: "task_items",
                columns: new[] { "list_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_task_items_list_id_is_completed_created_at",
                table: "task_items",
                columns: new[] { "list_id", "is_completed", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_list_items_list_id_is_completed_completed_at",
                table: "list_items",
                columns: new[] { "list_id", "is_completed", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_list_items_list_id_is_completed_created_at",
                table: "list_items",
                columns: new[] { "list_id", "is_completed", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_list_items_list_id_is_completed_returned_at",
                table: "list_items",
                columns: new[] { "list_id", "is_completed", "returned_to_pending_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_task_items_list_id_created_at",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "ix_task_items_list_id_is_completed_created_at",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "ix_list_items_list_id_is_completed_completed_at",
                table: "list_items");

            migrationBuilder.DropIndex(
                name: "ix_list_items_list_id_is_completed_created_at",
                table: "list_items");

            migrationBuilder.DropIndex(
                name: "ix_list_items_list_id_is_completed_returned_at",
                table: "list_items");
        }
    }
}
