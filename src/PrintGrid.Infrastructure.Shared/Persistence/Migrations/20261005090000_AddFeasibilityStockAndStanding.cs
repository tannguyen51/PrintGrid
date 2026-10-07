using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrintGrid.Infrastructure.Shared.Persistence;

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations;

[DbContext(typeof(PrintGridDbContext))]
[Migration("20261005090000_AddFeasibilityStockAndStanding")]
public class AddFeasibilityStockAndStanding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsInGoodStanding",
            schema: "scheduling",
            table: "labs",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "material_stocks",
            schema: "scheduling",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                LabId = table.Column<Guid>(type: "uuid", nullable: false),
                MaterialCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ColorCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                AvailableGrams = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_material_stocks", x => x.Id);
                table.ForeignKey(
                    name: "FK_material_stocks_labs_LabId",
                    column: x => x.LabId,
                    principalSchema: "scheduling",
                    principalTable: "labs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_material_stocks_LabId_MaterialCode_ColorCode",
            schema: "scheduling",
            table: "material_stocks",
            columns: new[] { "LabId", "MaterialCode", "ColorCode" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "material_stocks", schema: "scheduling");
        migrationBuilder.DropColumn(name: "IsInGoodStanding", schema: "scheduling", table: "labs");
    }
}
