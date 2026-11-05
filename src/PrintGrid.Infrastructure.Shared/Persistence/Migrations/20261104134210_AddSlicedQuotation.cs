using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSlicedQuotation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_material_reservations_JobId",
            schema: "scheduling",
            table: "material_reservations");

        migrationBuilder.AddColumn<decimal>(
            name: "BatchTotalAmount",
            schema: "customer",
            table: "quote_items",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CalculationJson",
            schema: "customer",
            table: "quote_items",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "QuotedMachineId",
            schema: "customer",
            table: "quote_items",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "BatchTotalAmount",
            schema: "customer",
            table: "order_items",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CalculationJson",
            schema: "customer",
            table: "order_items",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "QuotedMachineId",
            schema: "customer",
            table: "order_items",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProductionQuoteJson",
            schema: "scheduling",
            table: "jobs",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "QuotedMachineId",
            schema: "scheduling",
            table: "jobs",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_material_reservations_JobId_MaterialStockId",
            schema: "scheduling",
            table: "material_reservations",
            columns: new[] { "JobId", "MaterialStockId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_material_reservations_JobId_MaterialStockId",
            schema: "scheduling",
            table: "material_reservations");

        migrationBuilder.DropColumn(
            name: "BatchTotalAmount",
            schema: "customer",
            table: "quote_items");

        migrationBuilder.DropColumn(
            name: "CalculationJson",
            schema: "customer",
            table: "quote_items");

        migrationBuilder.DropColumn(
            name: "QuotedMachineId",
            schema: "customer",
            table: "quote_items");

        migrationBuilder.DropColumn(
            name: "BatchTotalAmount",
            schema: "customer",
            table: "order_items");

        migrationBuilder.DropColumn(
            name: "CalculationJson",
            schema: "customer",
            table: "order_items");

        migrationBuilder.DropColumn(
            name: "QuotedMachineId",
            schema: "customer",
            table: "order_items");

        migrationBuilder.DropColumn(
            name: "ProductionQuoteJson",
            schema: "scheduling",
            table: "jobs");

        migrationBuilder.DropColumn(
            name: "QuotedMachineId",
            schema: "scheduling",
            table: "jobs");

        migrationBuilder.CreateIndex(
            name: "IX_material_reservations_JobId",
            schema: "scheduling",
            table: "material_reservations",
            column: "JobId",
            unique: true);
    }
}
