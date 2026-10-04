using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuoteReviewGate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                schema: "customer",
                table: "quotes",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "AdjustmentReason",
                schema: "customer",
                table: "quotes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                schema: "customer",
                table: "quotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoApproved",
                schema: "customer",
                table: "quotes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EnginePromisedDeliveryDate",
                schema: "customer",
                table: "quotes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EngineTotalAmount",
                schema: "customer",
                table: "quotes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedBy",
                schema: "customer",
                table: "quotes",
                type: "uuid",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustmentReason",
                schema: "customer",
                table: "quotes");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "customer",
                table: "quotes");

            migrationBuilder.DropColumn(
                name: "AutoApproved",
                schema: "customer",
                table: "quotes");

            migrationBuilder.DropColumn(
                name: "EnginePromisedDeliveryDate",
                schema: "customer",
                table: "quotes");

            migrationBuilder.DropColumn(
                name: "EngineTotalAmount",
                schema: "customer",
                table: "quotes");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                schema: "customer",
                table: "quotes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                schema: "customer",
                table: "quotes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
