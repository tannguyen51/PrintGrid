using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitLabReliabilityScoringWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LabReliability",
                schema: "scheduling",
                table: "scoring_weight_configurations",
                newName: "OnTimeRate");

            migrationBuilder.AddColumn<decimal>(
                name: "FirstPassYield",
                schema: "scheduling",
                table: "scoring_weight_configurations",
                type: "numeric(8,6)",
                precision: 8,
                scale: 6,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstPassYield",
                schema: "scheduling",
                table: "scoring_weight_configurations");

            migrationBuilder.RenameColumn(
                name: "OnTimeRate",
                schema: "scheduling",
                table: "scoring_weight_configurations",
                newName: "LabReliability");
        }
    }
}
