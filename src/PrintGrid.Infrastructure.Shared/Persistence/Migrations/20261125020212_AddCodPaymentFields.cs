using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCodPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CodConfirmationDeadlineUtc",
                schema: "customer",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CodConfirmationRequestCount",
                schema: "customer",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CodConfirmationRequestedAtUtc",
                schema: "customer",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCodConfirmed",
                schema: "customer",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                schema: "customer",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "COD");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodConfirmationDeadlineUtc",
                schema: "customer",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CodConfirmationRequestCount",
                schema: "customer",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CodConfirmationRequestedAtUtc",
                schema: "customer",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "IsCodConfirmed",
                schema: "customer",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                schema: "customer",
                table: "orders");
        }
    }
}
