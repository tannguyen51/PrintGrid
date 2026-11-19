using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDispatchAndRequestedDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DispatchMode",
                schema: "customer",
                table: "orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<DateOnly>(
                name: "RequestedDeliveryDate",
                schema: "customer",
                table: "orders",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DispatchMode",
                schema: "customer",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "RequestedDeliveryDate",
                schema: "customer",
                table: "orders");
        }
    }
}
