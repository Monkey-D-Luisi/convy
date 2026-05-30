using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferentialIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_invites_household_id",
                table: "invites",
                newName: "ix_invites_household_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_items_assigned_to_user_id",
                table: "task_items",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_items_completed_by",
                table: "task_items",
                column: "completed_by");

            migrationBuilder.CreateIndex(
                name: "ix_task_items_created_by",
                table: "task_items",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_list_items_completed_by",
                table: "list_items",
                column: "completed_by");

            migrationBuilder.CreateIndex(
                name: "ix_list_items_created_by",
                table: "list_items",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_list_items_returned_to_pending_by",
                table: "list_items",
                column: "returned_to_pending_by");

            migrationBuilder.CreateIndex(
                name: "ix_invites_created_by",
                table: "invites",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_invites_used_by",
                table: "invites",
                column: "used_by");

            migrationBuilder.CreateIndex(
                name: "ix_households_created_by",
                table: "households",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_household_lists_created_by",
                table: "household_lists",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_activity_logs_performed_by",
                table: "activity_logs",
                column: "performed_by");

            migrationBuilder.AddForeignKey(
                name: "fk_activity_logs_households_household_id",
                table: "activity_logs",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_activity_logs_users_performed_by",
                table: "activity_logs",
                column: "performed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_device_tokens_users_user_id",
                table: "device_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_household_lists_households_household_id",
                table: "household_lists",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_household_lists_users_created_by",
                table: "household_lists",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_households_users_created_by",
                table: "households",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_invites_households_household_id",
                table: "invites",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_invites_users_created_by",
                table: "invites",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_invites_users_used_by",
                table: "invites",
                column: "used_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_list_items_household_lists_list_id",
                table: "list_items",
                column: "list_id",
                principalTable: "household_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_list_items_users_completed_by",
                table: "list_items",
                column: "completed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_list_items_users_created_by",
                table: "list_items",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_list_items_users_returned_to_pending_by",
                table: "list_items",
                column: "returned_to_pending_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_users_user_id",
                table: "notification_preferences",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_task_items_household_lists_list_id",
                table: "task_items",
                column: "list_id",
                principalTable: "household_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_task_items_users_assigned_to_user_id",
                table: "task_items",
                column: "assigned_to_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_task_items_users_completed_by",
                table: "task_items",
                column: "completed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_task_items_users_created_by",
                table: "task_items",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_activity_logs_households_household_id",
                table: "activity_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_activity_logs_users_performed_by",
                table: "activity_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_device_tokens_users_user_id",
                table: "device_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_household_lists_households_household_id",
                table: "household_lists");

            migrationBuilder.DropForeignKey(
                name: "fk_household_lists_users_created_by",
                table: "household_lists");

            migrationBuilder.DropForeignKey(
                name: "fk_households_users_created_by",
                table: "households");

            migrationBuilder.DropForeignKey(
                name: "fk_invites_households_household_id",
                table: "invites");

            migrationBuilder.DropForeignKey(
                name: "fk_invites_users_created_by",
                table: "invites");

            migrationBuilder.DropForeignKey(
                name: "fk_invites_users_used_by",
                table: "invites");

            migrationBuilder.DropForeignKey(
                name: "fk_list_items_household_lists_list_id",
                table: "list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_list_items_users_completed_by",
                table: "list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_list_items_users_created_by",
                table: "list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_list_items_users_returned_to_pending_by",
                table: "list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_users_user_id",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_task_items_household_lists_list_id",
                table: "task_items");

            migrationBuilder.DropForeignKey(
                name: "fk_task_items_users_assigned_to_user_id",
                table: "task_items");

            migrationBuilder.DropForeignKey(
                name: "fk_task_items_users_completed_by",
                table: "task_items");

            migrationBuilder.DropForeignKey(
                name: "fk_task_items_users_created_by",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "ix_task_items_assigned_to_user_id",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "ix_task_items_completed_by",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "ix_task_items_created_by",
                table: "task_items");

            migrationBuilder.DropIndex(
                name: "ix_list_items_completed_by",
                table: "list_items");

            migrationBuilder.DropIndex(
                name: "ix_list_items_created_by",
                table: "list_items");

            migrationBuilder.DropIndex(
                name: "ix_list_items_returned_to_pending_by",
                table: "list_items");

            migrationBuilder.DropIndex(
                name: "ix_invites_created_by",
                table: "invites");

            migrationBuilder.DropIndex(
                name: "ix_invites_used_by",
                table: "invites");

            migrationBuilder.DropIndex(
                name: "ix_households_created_by",
                table: "households");

            migrationBuilder.DropIndex(
                name: "ix_household_lists_created_by",
                table: "household_lists");

            migrationBuilder.DropIndex(
                name: "ix_activity_logs_performed_by",
                table: "activity_logs");

            migrationBuilder.RenameIndex(
                name: "ix_invites_household_id",
                table: "invites",
                newName: "IX_invites_household_id");
        }
    }
}
