using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteLabInventoryAndShopFloor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReorderPointGrams",
                schema: "scheduling",
                table: "material_stocks",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReservedGrams",
                schema: "scheduling",
                table: "material_stocks",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualMaterialGrams",
                schema: "scheduling",
                table: "jobs",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "material_reservations",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialStockId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservedGrams = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_material_reservations_labs_LabId",
                        column: x => x.LabId,
                        principalSchema: "scheduling",
                        principalTable: "labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_transactions",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialStockId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DeltaGrams = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    RunningTotalGrams = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_transactions_labs_LabId",
                        column: x => x.LabId,
                        principalSchema: "scheduling",
                        principalTable: "labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_material_reservations_JobId",
                schema: "scheduling",
                table: "material_reservations",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_reservations_LabId",
                schema: "scheduling",
                table: "material_reservations",
                column: "LabId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_LabId_CreatedAtUtc",
                schema: "scheduling",
                table: "stock_transactions",
                columns: new[] { "LabId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_TransactionCode",
                schema: "scheduling",
                table: "stock_transactions",
                column: "TransactionCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "material_reservations",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "stock_transactions",
                schema: "scheduling");

            migrationBuilder.DropColumn(
                name: "ReorderPointGrams",
                schema: "scheduling",
                table: "material_stocks");

            migrationBuilder.DropColumn(
                name: "ReservedGrams",
                schema: "scheduling",
                table: "material_stocks");

            migrationBuilder.DropColumn(
                name: "ActualMaterialGrams",
                schema: "scheduling",
                table: "jobs");
        }
    }
}
