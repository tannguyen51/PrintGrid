using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialScheduledActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ActivatedAtUtc",
                schema: "admin",
                table: "commercial_configuration_versions",
                newName: "activated_at_utc");

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_activation_at_utc",
                schema: "admin",
                table: "commercial_configuration_versions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scheduled_activation_at_utc",
                schema: "admin",
                table: "commercial_configuration_versions");

            migrationBuilder.RenameColumn(
                name: "activated_at_utc",
                schema: "admin",
                table: "commercial_configuration_versions",
                newName: "ActivatedAtUtc");
        }
    }
}
