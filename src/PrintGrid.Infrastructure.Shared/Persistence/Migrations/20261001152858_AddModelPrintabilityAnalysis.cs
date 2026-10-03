using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModelPrintabilityAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedMaterialGrams",
                schema: "customer",
                table: "models",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManifold",
                schema: "customer",
                table: "models",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrintable",
                schema: "customer",
                table: "models",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWatertight",
                schema: "customer",
                table: "models",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedMaterialGrams",
                schema: "customer",
                table: "models");

            migrationBuilder.DropColumn(
                name: "IsManifold",
                schema: "customer",
                table: "models");

            migrationBuilder.DropColumn(
                name: "IsPrintable",
                schema: "customer",
                table: "models");

            migrationBuilder.DropColumn(
                name: "IsWatertight",
                schema: "customer",
                table: "models");
        }
    }
}
