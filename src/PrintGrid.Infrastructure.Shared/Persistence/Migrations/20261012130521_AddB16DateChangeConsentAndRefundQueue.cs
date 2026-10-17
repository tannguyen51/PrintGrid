using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddB16DateChangeConsentAndRefundQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DecidedByCustomerId",
                schema: "scheduling",
                table: "date_change_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonCode",
                schema: "scheduling",
                table: "date_change_requests",
                type: "character varying(48)",
                maxLength: 48,
                nullable: false,
                defaultValue: "ReprintDeadlineUnrecoverable");

            migrationBuilder.CreateTable(
                name: "refund_requests",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateChangeRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentTransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refund_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refund_requests_DateChangeRequestId",
                schema: "customer",
                table: "refund_requests",
                column: "DateChangeRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refund_requests_Status_CreatedAtUtc",
                schema: "customer",
                table: "refund_requests",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refund_requests",
                schema: "customer");

            migrationBuilder.DropColumn(
                name: "DecidedByCustomerId",
                schema: "scheduling",
                table: "date_change_requests");

            migrationBuilder.DropColumn(
                name: "ReasonCode",
                schema: "scheduling",
                table: "date_change_requests");
        }
    }
}
