using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations;

[DbContext(typeof(PrintGridDbContext))]
[Migration("20261119000000_PreserveLegacyPrinterConfiguration")]
public sealed class PreserveLegacyPrinterConfiguration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The Cura library columns were added as nullable because printer configuration
        // is optional. Existing configured machines need empty resource IDs: EF treats
        // null required owned properties as a missing configuration when reading them.
        migrationBuilder.Sql("""
            UPDATE scheduling.machines
            SET "PrinterConfiguration_CuraDefinitionId" = COALESCE("PrinterConfiguration_CuraDefinitionId", ''),
                "PrinterConfiguration_CuraExtruderDefinitionId" = COALESCE("PrinterConfiguration_CuraExtruderDefinitionId", ''),
                "PrinterConfiguration_CuraVariantId" = COALESCE("PrinterConfiguration_CuraVariantId", ''),
                "PrinterConfiguration_CuraQualityId" = COALESCE("PrinterConfiguration_CuraQualityId", '')
            WHERE "PrinterConfiguration_Manufacturer" IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep valid resource IDs and legacy configurations when rolling back.
    }
}
