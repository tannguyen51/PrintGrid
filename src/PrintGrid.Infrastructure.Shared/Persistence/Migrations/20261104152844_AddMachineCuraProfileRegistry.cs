using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations;

/// <inheritdoc />
public partial class AddMachineCuraProfileRegistry : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid[]>(
            name: "CuraProfileIds",
            schema: "scheduling",
            table: "machines",
            type: "uuid[]",
            nullable: false,
            defaultValue: Array.Empty<Guid>());

        migrationBuilder.AddColumn<int>(
            name: "PrinterConfiguration_ExtruderCount",
            schema: "scheduling",
            table: "machines",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "PrinterConfiguration_FailureRate",
            schema: "scheduling",
            table: "machines",
            type: "numeric(7,6)",
            precision: 7,
            scale: 6,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "PrinterConfiguration_FilamentDiameterMm",
            schema: "scheduling",
            table: "machines",
            type: "numeric(6,3)",
            precision: 6,
            scale: 3,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "PrinterConfiguration_HeatedBed",
            schema: "scheduling",
            table: "machines",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PrinterConfiguration_Manufacturer",
            schema: "scheduling",
            table: "machines",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "PrinterConfiguration_MaxBedTemperatureC",
            schema: "scheduling",
            table: "machines",
            type: "numeric(6,2)",
            precision: 6,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "PrinterConfiguration_MaxNozzleTemperatureC",
            schema: "scheduling",
            table: "machines",
            type: "numeric(6,2)",
            precision: 6,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "PrinterConfiguration_MaximumColorSlots",
            schema: "scheduling",
            table: "machines",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "PrinterConfiguration_NozzleDiameterMm",
            schema: "scheduling",
            table: "machines",
            type: "numeric(6,3)",
            precision: 6,
            scale: 3,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "PrinterConfiguration_PricePerHour",
            schema: "scheduling",
            table: "machines",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProfileReviewReason",
            schema: "scheduling",
            table: "machines",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ProfileReviewedAtUtc",
            schema: "scheduling",
            table: "machines",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ProfileReviewedBy",
            schema: "scheduling",
            table: "machines",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProfileStatus",
            schema: "scheduling",
            table: "machines",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "MissingProfile");

        migrationBuilder.CreateTable(
            name: "cura_profiles",
            schema: "scheduling",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Manufacturer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                MachineModel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                MaterialCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Version = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                EngineVersion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                NozzleDiameterMm = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                FilamentDiameterMm = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                ExtruderCount = table.Column<int>(type: "integer", nullable: false),
                MaximumColorSlots = table.Column<int>(type: "integer", nullable: false),
                WidthMm = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                DepthMm = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                HeightMm = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                HeatedBed = table.Column<bool>(type: "boolean", nullable: false),
                RequiredNozzleTemperatureC = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                RequiredBedTemperatureC = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                SettingsJson = table.Column<string>(type: "jsonb", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cura_profiles", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_cura_profiles_Manufacturer_MachineModel_MaterialCode",
            schema: "scheduling",
            table: "cura_profiles",
            columns: ["Manufacturer", "MachineModel", "MaterialCode"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "cura_profiles",
            schema: "scheduling");

        migrationBuilder.DropColumn(
            name: "CuraProfileIds",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "PrinterConfiguration_ExtruderCount",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "PrinterConfiguration_FailureRate",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "PrinterConfiguration_FilamentDiameterMm",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "PrinterConfiguration_HeatedBed",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "PrinterConfiguration_Manufacturer",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "PrinterConfiguration_MaxBedTemperatureC",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "PrinterConfiguration_MaxNozzleTemperatureC",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "PrinterConfiguration_MaximumColorSlots",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "PrinterConfiguration_NozzleDiameterMm",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "PrinterConfiguration_PricePerHour",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "ProfileReviewReason",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "ProfileReviewedAtUtc",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "ProfileReviewedBy",
            schema: "scheduling",
            table: "machines");

        migrationBuilder.DropColumn(
            name: "ProfileStatus",
            schema: "scheduling",
            table: "machines");
    }
}
