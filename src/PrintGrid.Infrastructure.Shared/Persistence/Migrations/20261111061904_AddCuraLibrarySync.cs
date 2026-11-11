using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCuraLibrarySync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrinterConfiguration_CuraDefinitionId",
                schema: "scheduling",
                table: "machines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrinterConfiguration_CuraExtruderDefinitionId",
                schema: "scheduling",
                table: "machines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrinterConfiguration_CuraQualityId",
                schema: "scheduling",
                table: "machines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrinterConfiguration_CuraVariantId",
                schema: "scheduling",
                table: "machines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuraDefinitionId",
                schema: "scheduling",
                table: "cura_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CuraExtruderDefinitionId",
                schema: "scheduling",
                table: "cura_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CuraQualityId",
                schema: "scheduling",
                table: "cura_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CuraVariantId",
                schema: "scheduling",
                table: "cura_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                schema: "scheduling",
                table: "cura_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrinterConfiguration_CuraDefinitionId",
                schema: "scheduling",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "PrinterConfiguration_CuraExtruderDefinitionId",
                schema: "scheduling",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "PrinterConfiguration_CuraQualityId",
                schema: "scheduling",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "PrinterConfiguration_CuraVariantId",
                schema: "scheduling",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "CuraDefinitionId",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "CuraExtruderDefinitionId",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "CuraQualityId",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "CuraVariantId",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                schema: "scheduling",
                table: "cura_profiles");
        }
    }
}
