using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimationCalibration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CalibrationComputedAtUtc",
                schema: "scheduling",
                table: "machines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CalibrationFactor",
                schema: "scheduling",
                table: "machines",
                type: "numeric(5,3)",
                precision: 5,
                scale: 3,
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CalibrationMape",
                schema: "scheduling",
                table: "machines",
                type: "numeric(6,4)",
                precision: 6,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CalibrationSampleCount",
                schema: "scheduling",
                table: "machines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "calibration_samples",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EstimatedMinutes = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ActualMinutes = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calibration_samples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_calibration_samples_machines_MachineId",
                        column: x => x.MachineId,
                        principalSchema: "scheduling",
                        principalTable: "machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_calibration_samples_JobId",
                schema: "scheduling",
                table: "calibration_samples",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_calibration_samples_MachineId_CreatedAtUtc",
                schema: "scheduling",
                table: "calibration_samples",
                columns: new[] { "MachineId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calibration_samples",
                schema: "scheduling");

            migrationBuilder.DropColumn(
                name: "CalibrationComputedAtUtc",
                schema: "scheduling",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "CalibrationFactor",
                schema: "scheduling",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "CalibrationMape",
                schema: "scheduling",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "CalibrationSampleCount",
                schema: "scheduling",
                table: "machines");
        }
    }
}
