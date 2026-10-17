using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLabShipmentBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransferDaysOfWeek",
                schema: "scheduling",
                table: "labs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "Monday,Wednesday,Friday");

            migrationBuilder.AddColumn<string>(
                name: "TransportZone",
                schema: "scheduling",
                table: "labs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "LOCAL");

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentBatchId",
                schema: "scheduling",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "shipment_batches",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledTransferDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TransportZone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WeightClass = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TotalWeightGrams = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ZoneRate = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    WeightMultiplier = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    TransportAllowance = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    JobCount = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    WaybillNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SealedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SealedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_batches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_jobs_ShipmentBatchId",
                schema: "scheduling",
                table: "jobs",
                column: "ShipmentBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_batches_LabId_ScheduledTransferDate",
                schema: "scheduling",
                table: "shipment_batches",
                columns: new[] { "LabId", "ScheduledTransferDate" });

            migrationBuilder.CreateIndex(
                name: "IX_shipment_batches_WaybillNumber",
                schema: "scheduling",
                table: "shipment_batches",
                column: "WaybillNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_jobs_shipment_batches_ShipmentBatchId",
                schema: "scheduling",
                table: "jobs",
                column: "ShipmentBatchId",
                principalSchema: "scheduling",
                principalTable: "shipment_batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_jobs_shipment_batches_ShipmentBatchId",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropTable(
                name: "shipment_batches",
                schema: "scheduling");

            migrationBuilder.DropIndex(
                name: "IX_jobs_ShipmentBatchId",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "TransferDaysOfWeek",
                schema: "scheduling",
                table: "labs");

            migrationBuilder.DropColumn(
                name: "TransportZone",
                schema: "scheduling",
                table: "labs");

            migrationBuilder.DropColumn(
                name: "ShipmentBatchId",
                schema: "scheduling",
                table: "jobs");
        }
    }
}
