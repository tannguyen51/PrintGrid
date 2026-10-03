using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GoldenPathR1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "shared");

            migrationBuilder.AddColumn<string>(
                name: "PlacementBasis",
                schema: "customer",
                table: "quotes",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricingVersion",
                schema: "customer",
                table: "quotes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BoundingDepthMm",
                schema: "customer",
                table: "quote_items",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BoundingHeightMm",
                schema: "customer",
                table: "quote_items",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BoundingWidthMm",
                schema: "customer",
                table: "quote_items",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MachineTimeCostAmount",
                schema: "customer",
                table: "quote_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaterialCostAmount",
                schema: "customer",
                table: "quote_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BoundingDepthMm",
                schema: "customer",
                table: "order_items",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BoundingHeightMm",
                schema: "customer",
                table: "order_items",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BoundingWidthMm",
                schema: "customer",
                table: "order_items",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedMaterialGrams",
                schema: "customer",
                table: "order_items",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedPrintMinutes",
                schema: "customer",
                table: "order_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                schema: "customer",
                table: "models",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_outbox",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_outbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_Status_CreatedAtUtc",
                schema: "shared",
                table: "email_outbox",
                columns: new[] { "Status", "CreatedAtUtc" });

            // NFR-REL-004: machine double-booking is IMPOSSIBLE BY CONSTRUCTION —
            // overlapping assigned windows for the same machine are rejected by the DB itself,
            // regardless of how many concurrent placers race (FR-SCHED-006 AC).
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql("""
                ALTER TABLE scheduling.jobs
                    ADD CONSTRAINT jobs_no_machine_overlap
                    EXCLUDE USING gist (
                        "MachineId" WITH =,
                        tstzrange("PlannedStartUtc", "PlannedEndUtc") WITH &&
                    )
                    WHERE (
                        "MachineId" IS NOT NULL
                        AND "PlannedStartUtc" IS NOT NULL
                        AND "PlannedEndUtc" IS NOT NULL
                        AND "Status" IN ('Assigned','Accepted','InProgress')
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE scheduling.jobs DROP CONSTRAINT IF EXISTS jobs_no_machine_overlap;");

            migrationBuilder.DropTable(
                name: "email_outbox",
                schema: "shared");

            migrationBuilder.DropColumn(
                name: "PlacementBasis",
                schema: "customer",
                table: "quotes");

            migrationBuilder.DropColumn(
                name: "PricingVersion",
                schema: "customer",
                table: "quotes");

            migrationBuilder.DropColumn(
                name: "BoundingDepthMm",
                schema: "customer",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "BoundingHeightMm",
                schema: "customer",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "BoundingWidthMm",
                schema: "customer",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "MachineTimeCostAmount",
                schema: "customer",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "MaterialCostAmount",
                schema: "customer",
                table: "quote_items");

            migrationBuilder.DropColumn(
                name: "BoundingDepthMm",
                schema: "customer",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "BoundingHeightMm",
                schema: "customer",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "BoundingWidthMm",
                schema: "customer",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "EstimatedMaterialGrams",
                schema: "customer",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "EstimatedPrintMinutes",
                schema: "customer",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "Sha256",
                schema: "customer",
                table: "models");
        }
    }
}
