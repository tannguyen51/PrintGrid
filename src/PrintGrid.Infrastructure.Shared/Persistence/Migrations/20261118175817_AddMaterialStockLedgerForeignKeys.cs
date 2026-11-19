using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialStockLedgerForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_MaterialStockId",
                schema: "scheduling",
                table: "stock_transactions",
                column: "MaterialStockId");

            migrationBuilder.CreateIndex(
                name: "IX_material_reservations_MaterialStockId",
                schema: "scheduling",
                table: "material_reservations",
                column: "MaterialStockId");

            migrationBuilder.AddForeignKey(
                name: "FK_material_reservations_material_stocks_MaterialStockId",
                schema: "scheduling",
                table: "material_reservations",
                column: "MaterialStockId",
                principalSchema: "scheduling",
                principalTable: "material_stocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_transactions_material_stocks_MaterialStockId",
                schema: "scheduling",
                table: "stock_transactions",
                column: "MaterialStockId",
                principalSchema: "scheduling",
                principalTable: "material_stocks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_material_reservations_material_stocks_MaterialStockId",
                schema: "scheduling",
                table: "material_reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_transactions_material_stocks_MaterialStockId",
                schema: "scheduling",
                table: "stock_transactions");

            migrationBuilder.DropIndex(
                name: "IX_stock_transactions_MaterialStockId",
                schema: "scheduling",
                table: "stock_transactions");

            migrationBuilder.DropIndex(
                name: "IX_material_reservations_MaterialStockId",
                schema: "scheduling",
                table: "material_reservations");
        }
    }
}
