using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "task_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_items", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_task_items_list_id",
                table: "task_items",
                column: "list_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_items_list_id_is_completed",
                table: "task_items",
                columns: new[] { "list_id", "is_completed" });

            migrationBuilder.Sql("""
                INSERT INTO task_items (
                    id,
                    title,
                    note,
                    list_id,
                    created_by,
                    created_at,
                    is_completed,
                    completed_by,
                    completed_at)
                SELECT
                    li.id,
                    li.title,
                    li.note,
                    li.list_id,
                    li.created_by,
                    li.created_at,
                    li.is_completed,
                    li.completed_by,
                    li.completed_at
                FROM list_items li
                INNER JOIN household_lists hl ON hl.id = li.list_id
                WHERE hl.type = 'Tasks';
                """);

            migrationBuilder.Sql("""
                UPDATE activity_logs
                SET entity_type = 'Task'
                WHERE entity_type = 'Item'
                  AND entity_id IN (
                      SELECT ti.id
                      FROM task_items ti
                  );
                """);

            migrationBuilder.Sql("""
                DELETE FROM list_items li
                USING household_lists hl
                WHERE hl.id = li.list_id
                  AND hl.type = 'Tasks';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO list_items (
                    id,
                    title,
                    quantity,
                    unit,
                    note,
                    list_id,
                    created_by,
                    created_at,
                    is_completed,
                    completed_by,
                    completed_at,
                    recurrence_frequency,
                    recurrence_interval,
                    next_due_date)
                SELECT
                    id,
                    title,
                    NULL,
                    NULL,
                    note,
                    list_id,
                    created_by,
                    created_at,
                    is_completed,
                    completed_by,
                    completed_at,
                    NULL,
                    NULL,
                    NULL
                FROM task_items;
                """);

            migrationBuilder.Sql("""
                UPDATE activity_logs
                SET entity_type = 'Item'
                WHERE entity_type = 'Task'
                  AND entity_id IN (
                      SELECT ti.id
                      FROM task_items ti
                  );
                """);

            migrationBuilder.DropTable(
                name: "task_items");
        }
    }
}
