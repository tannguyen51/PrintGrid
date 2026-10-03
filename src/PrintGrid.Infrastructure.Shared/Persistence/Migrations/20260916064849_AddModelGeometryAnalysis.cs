using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModelGeometryAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BoundingDepthMm",
                schema: "customer",
                table: "models",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BoundingHeightMm",
                schema: "customer",
                table: "models",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BoundingWidthMm",
                schema: "customer",
                table: "models",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedPrintMinutes",
                schema: "customer",
                table: "models",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeometryMessage",
                schema: "customer",
                table: "models",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeometryStatus",
                schema: "customer",
                table: "models",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "VolumeCm3",
                schema: "customer",
                table: "models",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoundingDepthMm",
                schema: "customer",
                table: "models");

            migrationBuilder.DropColumn(
                name: "BoundingHeightMm",
                schema: "customer",
                table: "models");

            migrationBuilder.DropColumn(
                name: "BoundingWidthMm",
                schema: "customer",
                table: "models");

            migrationBuilder.DropColumn(
                name: "EstimatedPrintMinutes",
                schema: "customer",
                table: "models");

            migrationBuilder.DropColumn(
                name: "GeometryMessage",
                schema: "customer",
                table: "models");

            migrationBuilder.DropColumn(
                name: "GeometryStatus",
                schema: "customer",
                table: "models");

            migrationBuilder.DropColumn(
                name: "VolumeCm3",
                schema: "customer",
                table: "models");
        }
    }
}
