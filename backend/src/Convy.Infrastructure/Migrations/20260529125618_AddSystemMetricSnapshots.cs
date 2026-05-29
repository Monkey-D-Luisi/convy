using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemMetricSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "system_metric_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    disk_free_bytes = table.Column<long>(type: "bigint", nullable: true),
                    disk_total_bytes = table.Column<long>(type: "bigint", nullable: true),
                    memory_available_bytes = table.Column<long>(type: "bigint", nullable: true),
                    memory_total_bytes = table.Column<long>(type: "bigint", nullable: true),
                    load_average_1m = table.Column<double>(type: "double precision", nullable: true),
                    uptime_seconds = table.Column<long>(type: "bigint", nullable: true),
                    postgres_data_size_bytes = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_metric_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_system_metric_snapshots_captured_at",
                table: "system_metric_snapshots",
                column: "captured_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_metric_snapshots");
        }
    }
}
