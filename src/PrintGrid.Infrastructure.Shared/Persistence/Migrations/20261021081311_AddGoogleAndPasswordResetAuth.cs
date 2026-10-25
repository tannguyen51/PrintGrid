using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAndPasswordResetAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleSubject",
                schema: "customer",
                table: "customers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                schema: "customer",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                schema: "customer",
                table: "customers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionVersion",
                schema: "customer",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_customers_GoogleSubject",
                schema: "customer",
                table: "customers",
                column: "GoogleSubject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customers_GoogleSubject",
                schema: "customer",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "GoogleSubject",
                schema: "customer",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                schema: "customer",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                schema: "customer",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "SessionVersion",
                schema: "customer",
                table: "customers");
        }
    }
}
