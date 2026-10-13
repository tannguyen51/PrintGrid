using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrintGrid.Infrastructure.Shared.Persistence;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations;

[DbContext(typeof(PrintGridDbContext))]
[Migration("20261012070000_AddWeek4LabAndHubConsolidation")]
public class AddWeek4LabAndHubConsolidation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
            schema: "scheduling",
            table: "labs",
            type: "character varying(24)",
            maxLength: 24,
            nullable: false,
            defaultValue: "Active");

        migrationBuilder.AddColumn<string>(
            name: "OperatingHours",
            schema: "scheduling",
            table: "labs",
            type: "character varying(120)",
            maxLength: 120,
            nullable: false,
            defaultValue: "T2-T7 08:00-18:00");

        migrationBuilder.AddColumn<Guid>(
            name: "OwnerUserId",
            schema: "scheduling",
            table: "labs",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ContactPhone",
            schema: "scheduling",
            table: "labs",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Address",
            schema: "scheduling",
            table: "labs",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RejectionReason",
            schema: "scheduling",
            table: "labs",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ApprovedAtUtc",
            schema: "scheduling",
            table: "labs",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ApprovedByUserId",
            schema: "scheduling",
            table: "labs",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_labs_Status",
            schema: "scheduling",
            table: "labs",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_labs_OwnerUserId",
            schema: "scheduling",
            table: "labs",
            column: "OwnerUserId");

        migrationBuilder.AddColumn<DateTime>(
            name: "MaintenanceFromUtc",
            schema: "scheduling",
            table: "machines",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "MaintenanceUntilUtc",
            schema: "scheduling",
            table: "machines",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MaintenanceReason",
            schema: "scheduling",
            table: "machines",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ReadyForPackingAtUtc",
            schema: "customer",
            table: "orders",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ConsolidatedShipmentId",
            schema: "customer",
            table: "orders",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "RefundedAmount",
            schema: "customer",
            table: "orders",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.CreateIndex(
            name: "IX_orders_ConsolidatedShipmentId",
            schema: "customer",
            table: "orders",
            column: "ConsolidatedShipmentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_orders_ConsolidatedShipmentId", schema: "customer", table: "orders");
        migrationBuilder.DropColumn(name: "RefundedAmount", schema: "customer", table: "orders");
        migrationBuilder.DropColumn(name: "ConsolidatedShipmentId", schema: "customer", table: "orders");
        migrationBuilder.DropColumn(name: "ReadyForPackingAtUtc", schema: "customer", table: "orders");

        migrationBuilder.DropColumn(name: "MaintenanceReason", schema: "scheduling", table: "machines");
        migrationBuilder.DropColumn(name: "MaintenanceUntilUtc", schema: "scheduling", table: "machines");
        migrationBuilder.DropColumn(name: "MaintenanceFromUtc", schema: "scheduling", table: "machines");

        migrationBuilder.DropIndex(name: "IX_labs_OwnerUserId", schema: "scheduling", table: "labs");
        migrationBuilder.DropIndex(name: "IX_labs_Status", schema: "scheduling", table: "labs");
        migrationBuilder.DropColumn(name: "ApprovedByUserId", schema: "scheduling", table: "labs");
        migrationBuilder.DropColumn(name: "ApprovedAtUtc", schema: "scheduling", table: "labs");
        migrationBuilder.DropColumn(name: "RejectionReason", schema: "scheduling", table: "labs");
        migrationBuilder.DropColumn(name: "Address", schema: "scheduling", table: "labs");
        migrationBuilder.DropColumn(name: "ContactPhone", schema: "scheduling", table: "labs");
        migrationBuilder.DropColumn(name: "OwnerUserId", schema: "scheduling", table: "labs");
        migrationBuilder.DropColumn(name: "OperatingHours", schema: "scheduling", table: "labs");
        migrationBuilder.DropColumn(name: "Status", schema: "scheduling", table: "labs");
    }
}

