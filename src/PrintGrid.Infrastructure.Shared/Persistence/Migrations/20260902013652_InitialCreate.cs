using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "customer");

            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    required_width_mm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    required_depth_mm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    required_height_mm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    material_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    color_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    layer_height_mm = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    tolerance_mm = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false),
                    technology = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    material_grams = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InternalDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EstimatedPrintMinutes = table.Column<int>(type: "integer", nullable: false),
                    ActualPrintMinutes = table.Column<int>(type: "integer", nullable: true),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: true),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlannedStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlannedEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "labs",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OnTimeDeliveryRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FirstPassYield = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    TransitDaysToHub = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PromisedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    delivery_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    delivery_ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    delivery_district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    delivery_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    delivery_postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    delivery_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentTransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PromisedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "machines",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Technology = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    build_width_mm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    build_depth_mm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    build_height_mm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    MinLayerHeightMm = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    AchievableToleranceMm = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SpeedFactor = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false),
                    supported_materials = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_machines_labs_LabId",
                        column: x => x.LabId,
                        principalSchema: "scheduling",
                        principalTable: "labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    material_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    color_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    layer_height_mm = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    infill_percent = table.Column<int>(type: "integer", nullable: false),
                    tolerance_mm = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false),
                    unit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "customer",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quote_items",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    material_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    color_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    layer_height_mm = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    infill_percent = table.Column<int>(type: "integer", nullable: false),
                    tolerance_mm = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false),
                    unit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    EstimatedPrintMinutes = table.Column<int>(type: "integer", nullable: false),
                    EstimatedMaterialGrams = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quote_items_quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "customer",
                        principalTable: "quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customers_Email",
                schema: "customer",
                table: "customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_InternalDueDate",
                schema: "scheduling",
                table: "jobs",
                column: "InternalDueDate");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_OrderItemId",
                schema: "scheduling",
                table: "jobs",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_jobs_Status_MachineId",
                schema: "scheduling",
                table: "jobs",
                columns: new[] { "Status", "MachineId" });

            migrationBuilder.CreateIndex(
                name: "IX_labs_IsActive",
                schema: "scheduling",
                table: "labs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_machines_LabId_Status",
                schema: "scheduling",
                table: "machines",
                columns: new[] { "LabId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_OrderId",
                schema: "customer",
                table: "order_items",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_CustomerId_Status",
                schema: "customer",
                table: "orders",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_OrderNumber",
                schema: "customer",
                table: "orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_items_QuoteId",
                schema: "customer",
                table: "quote_items",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_CustomerId_Status",
                schema: "customer",
                table: "quotes",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_quotes_ExpiresAt",
                schema: "customer",
                table: "quotes",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customers",
                schema: "customer");

            migrationBuilder.DropTable(
                name: "jobs",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "machines",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "order_items",
                schema: "customer");

            migrationBuilder.DropTable(
                name: "quote_items",
                schema: "customer");

            migrationBuilder.DropTable(
                name: "labs",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "customer");

            migrationBuilder.DropTable(
                name: "quotes",
                schema: "customer");
        }
    }
}
