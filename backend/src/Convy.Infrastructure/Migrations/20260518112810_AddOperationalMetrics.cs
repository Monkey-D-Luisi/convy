using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "list_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.CreateTable(
                name: "backup_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    backup_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    verification_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "voice_parse_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    audio_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    audio_duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    parsed_items_count = table.Column<int>(type: "integer", nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: true),
                    output_tokens = table.Column<int>(type: "integer", nullable: true),
                    cached_tokens = table.Column<int>(type: "integer", nullable: true),
                    reasoning_tokens = table.Column<int>(type: "integer", nullable: true),
                    estimated_cost_micros = table.Column<long>(type: "bigint", nullable: true),
                    latency_ms = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voice_parse_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_list_items_source_created_at",
                table: "list_items",
                columns: new[] { "source", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_backup_runs_started_at",
                table: "backup_runs",
                column: "started_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_backup_runs_status_started_at",
                table: "backup_runs",
                columns: new[] { "status", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_voice_parse_events_created_at",
                table: "voice_parse_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_voice_parse_events_household_id_created_at",
                table: "voice_parse_events",
                columns: new[] { "household_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_voice_parse_events_status_created_at",
                table: "voice_parse_events",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "backup_runs");

            migrationBuilder.DropTable(
                name: "voice_parse_events");

            migrationBuilder.DropIndex(
                name: "ix_list_items_source_created_at",
                table: "list_items");

            migrationBuilder.DropColumn(
                name: "source",
                table: "list_items");
        }
    }
}
