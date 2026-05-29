using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskMetadataAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assigned_to_user_id",
                table: "task_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "due_date",
                table: "task_items",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "priority",
                table: "task_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.AddColumn<DateTime>(
                name: "reminder_at_utc",
                table: "task_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reminder_sent_at_utc",
                table: "task_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "task_reminders",
                table: "notification_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_items_list_id_assigned_to_user_id",
                table: "task_items",
                columns: new[] { "list_id", "assigned_to_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_items_list_id_due_date",
                table: "task_items",
                columns: new[] { "list_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_task_items_reminder_at_utc_reminder_sent_at_utc",
                table: "task_items",
                columns: new[] { "reminder_at_utc", "reminder_sent_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_task_items_list_id_assigned_to_user_id",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "ix_task_items_list_id_due_date",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "ix_task_items_reminder_at_utc_reminder_sent_at_utc",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "assigned_to_user_id",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "due_date",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "reminder_at_utc",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "reminder_sent_at_utc",
                table: "task_items");

            migrationBuilder.DropColumn(
                name: "task_reminders",
                table: "notification_preferences");
        }
    }
}
