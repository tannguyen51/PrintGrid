using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineEnclosedChamber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrinterConfiguration_EnclosedChamber",
                schema: "scheduling",
                table: "machines",
                type: "boolean",
                nullable: true);
            // Existing hardware has no declaration of an enclosure. Do not infer it from a heated bed.
            migrationBuilder.Sql("""
                UPDATE scheduling.machines SET "PrinterConfiguration_EnclosedChamber" = FALSE
                WHERE "PrinterConfiguration_Manufacturer" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrinterConfiguration_EnclosedChamber",
                schema: "scheduling",
                table: "machines");
        }
    }
}
