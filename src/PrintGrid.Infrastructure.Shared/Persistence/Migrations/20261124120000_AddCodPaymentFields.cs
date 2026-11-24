using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCodPaymentFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PaymentMethod",
            table: "Orders",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "COD");

        migrationBuilder.AddColumn<bool>(
            name: "IsCodConfirmed",
            table: "Orders",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "CodConfirmationRequestedAtUtc",
            table: "Orders",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CodConfirmationDeadlineUtc",
            table: "Orders",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CodConfirmationRequestCount",
            table: "Orders",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CodConfirmationRequestCount",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "CodConfirmationDeadlineUtc",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "CodConfirmationRequestedAtUtc",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "IsCodConfirmed",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "PaymentMethod",
            table: "Orders");
    }
}
