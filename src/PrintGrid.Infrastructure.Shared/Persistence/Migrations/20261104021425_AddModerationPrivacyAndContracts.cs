using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations;

/// <inheritdoc />
public partial class AddModerationPrivacyAndContracts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CustomerPlacementSummary",
            schema: "customer",
            table: "quotes",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ContractNumber",
            schema: "customer",
            table: "orders",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "TermsAccepted",
            schema: "customer",
            table: "orders",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "TermsAcceptedAtUtc",
            schema: "customer",
            table: "orders",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReviewReason",
            schema: "customer",
            table: "models",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReviewStatus",
            schema: "customer",
            table: "models",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Approved");

        migrationBuilder.AddColumn<DateTime>(
            name: "ReviewedAtUtc",
            schema: "customer",
            table: "models",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ReviewedBy",
            schema: "customer",
            table: "models",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReviewedSha256",
            schema: "customer",
            table: "models",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "District",
            schema: "scheduling",
            table: "labs",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        // Existing models are grandfathered as approved (they are already in customers'
        // libraries and orders). Removing the default here means every new insert must
        // send the entity value, which is Pending.
        migrationBuilder.AlterColumn<string>(
            name: "ReviewStatus",
            schema: "customer",
            table: "models",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(16)");

        // Historical orders accepted the terms they were placed under.
        migrationBuilder.Sql(
            "UPDATE customer.orders SET \"TermsAccepted\" = TRUE, \"TermsAcceptedAtUtc\" = \"CreatedAt\" WHERE \"TermsAcceptedAtUtc\" IS NULL;");

        // Confirmed orders get a deterministic contract number identical to
        // Order.BuildContractNumber (date + first 12 hex chars of the order id).
        migrationBuilder.Sql(
            "UPDATE customer.orders " +
            "SET \"ContractNumber\" = 'PG-CT-' || to_char(\"ConfirmedAt\" AT TIME ZONE 'UTC','YYYYMMDD') || '-' || upper(substr(replace(\"Id\"::text,'-',''),1,12)) " +
            "WHERE \"ConfirmedAt\" IS NOT NULL AND \"ContractNumber\" IS NULL;");

        migrationBuilder.CreateIndex(
            name: "IX_orders_ContractNumber",
            schema: "customer",
            table: "orders",
            column: "ContractNumber",
            unique: true,
            filter: "\"ContractNumber\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_models_ReviewStatus_CreatedAt",
            schema: "customer",
            table: "models",
            columns: ["ReviewStatus", "CreatedAt"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_orders_ContractNumber",
            schema: "customer",
            table: "orders");

        migrationBuilder.DropIndex(
            name: "IX_models_ReviewStatus_CreatedAt",
            schema: "customer",
            table: "models");

        migrationBuilder.DropColumn(
            name: "CustomerPlacementSummary",
            schema: "customer",
            table: "quotes");

        migrationBuilder.DropColumn(
            name: "ContractNumber",
            schema: "customer",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "TermsAccepted",
            schema: "customer",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "TermsAcceptedAtUtc",
            schema: "customer",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "ReviewReason",
            schema: "customer",
            table: "models");

        migrationBuilder.DropColumn(
            name: "ReviewStatus",
            schema: "customer",
            table: "models");

        migrationBuilder.DropColumn(
            name: "ReviewedAtUtc",
            schema: "customer",
            table: "models");

        migrationBuilder.DropColumn(
            name: "ReviewedBy",
            schema: "customer",
            table: "models");

        migrationBuilder.DropColumn(
            name: "ReviewedSha256",
            schema: "customer",
            table: "models");

        migrationBuilder.DropColumn(
            name: "District",
            schema: "scheduling",
            table: "labs");
    }
}
