using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiUsageEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_usage_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: true),
                    feature = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    operation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    error_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    latency_ms = table.Column<long>(type: "bigint", nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: true),
                    output_tokens = table.Column<int>(type: "integer", nullable: true),
                    cached_tokens = table.Column<int>(type: "integer", nullable: true),
                    reasoning_tokens = table.Column<int>(type: "integer", nullable: true),
                    audio_tokens = table.Column<int>(type: "integer", nullable: true),
                    text_tokens = table.Column<int>(type: "integer", nullable: true),
                    audio_duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    estimated_cost_micros = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_usage_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_usage_events_created_at",
                table: "ai_usage_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_ai_usage_events_feature_operation_created_at",
                table: "ai_usage_events",
                columns: new[] { "feature", "operation", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_usage_events_household_id_created_at",
                table: "ai_usage_events",
                columns: new[] { "household_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_usage_events");
        }
    }
}
