using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerMetricScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "container_metrics_json",
                table: "system_metric_snapshots",
                type: "character varying(16384)",
                maxLength: 16384,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metric_scope",
                table: "system_metric_snapshots",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "legacy-host");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "container_metrics_json",
                table: "system_metric_snapshots");

            migrationBuilder.DropColumn(
                name: "metric_scope",
                table: "system_metric_snapshots");
        }
    }
}
