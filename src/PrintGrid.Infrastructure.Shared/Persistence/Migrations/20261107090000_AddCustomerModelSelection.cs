using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations;

/// <inheritdoc />
/// Adds customer model selection schema per doc "PrintGrid – Phương án xử lý khi khách muốn chọn máy in, model máy"
/// Sections 3 (tech constraints), 4 (model pinning), 5 (reorder), 6 (DB additions).
public partial class AddCustomerModelSelection : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ────────────────────────────────────────────────────────────────
        // quote_items — profile traceability + pinch surcharge comparison (§6 Quote)
        // ────────────────────────────────────────────────────────────────
        migrationBuilder.AddColumn<Guid>(
            name: "profile_id",
            schema: "customer",
            table: "quote_items",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "profile_version",
            schema: "customer",
            table: "quote_items",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "SurchargePinchCoef",
            schema: "customer",
            table: "quote_items",
            type: "numeric(5,3)",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "AlternativePriceVnd",
            schema: "customer",
            table: "quote_items",
            type: "numeric(18,0)",
            nullable: true);

        migrationBuilder.AddColumn<DateOnly>(
            name: "AlternativeDeliveryDate",
            schema: "customer",
            table: "quote_items",
            type: "date",
            nullable: true);

        // ────────────────────────────────────────────────────────────────
        // order_items — tech constraints + machine binding (§6 OrderItem)
        // ────────────────────────────────────────────────────────────────
        migrationBuilder.AddColumn<string>(
            name: "TechConstraintsJson",
            schema: "customer",
            table: "order_items",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ModelBindingKind",
            schema: "customer",
            table: "order_items",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "không");

        migrationBuilder.AddColumn<Guid>(
            name: "PinnedModelId",
            schema: "customer",
            table: "order_items",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BindingLevel",
            schema: "customer",
            table: "order_items",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "bắt_buộc");

        migrationBuilder.AddColumn<Guid>(
            name: "ReferenceOrderLineId",
            schema: "customer",
            table: "order_items",
            type: "uuid",
            nullable: true);

        // ────────────────────────────────────────────────────────────────
        // machines — pin permission toggle (§6.5, BR-04)
        // ────────────────────────────────────────────────────────────────
        migrationBuilder.AddColumn<bool>(
            name: "AllowsCustomerPinning",
            schema: "scheduling",
            table: "machines",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        // ────────────────────────────────────────────────────────────────
        // machine_assignment_decisions — audit log for dispute resolution (§6.6)
        // ────────────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "machine_assignment_decisions",
            schema: "scheduling",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                JobId = table.Column<Guid>(type: "uuid", nullable: false),
                MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                Decision = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_machine_assignment_decisions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_machine_assignment_decisions_JobId_MachineId",
            schema: "scheduling",
            table: "machine_assignment_decisions",
            columns: new[] { "JobId", "MachineId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // ── quote_items ──
        migrationBuilder.DropColumn(name: "AlternativeDeliveryDate", schema: "customer", table: "quote_items");
        migrationBuilder.DropColumn(name: "AlternativePriceVnd", schema: "customer", table: "quote_items");
        migrationBuilder.DropColumn(name: "SurchargePinchCoef", schema: "customer", table: "quote_items");
        migrationBuilder.DropColumn(name: "profile_version", schema: "customer", table: "quote_items");
        migrationBuilder.DropColumn(name: "profile_id", schema: "customer", table: "quote_items");

        // ── order_items ──
        migrationBuilder.DropColumn(name: "ReferenceOrderLineId", schema: "customer", table: "order_items");
        migrationBuilder.DropColumn(name: "BindingLevel", schema: "customer", table: "order_items");
        migrationBuilder.DropColumn(name: "PinnedModelId", schema: "customer", table: "order_items");
        migrationBuilder.DropColumn(name: "ModelBindingKind", schema: "customer", table: "order_items");
        migrationBuilder.DropColumn(name: "TechConstraintsJson", schema: "customer", table: "order_items");

        // ── machines ──
        migrationBuilder.DropColumn(name: "AllowsCustomerPinning", schema: "scheduling", table: "machines");

        // ── machine_assignment_decisions ──
        migrationBuilder.DropTable(name: "machine_assignment_decisions", schema: "scheduling");
    }
}
