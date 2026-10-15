using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLabApprovalAndCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentJobs",
                schema: "scheduling",
                table: "labs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "scheduling",
                table: "labs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_labs_OwnerUserId",
                schema: "scheduling",
                table: "labs",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_labs_OwnerUserId",
                schema: "scheduling",
                table: "labs");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentJobs",
                schema: "scheduling",
                table: "labs");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "scheduling",
                table: "labs");
        }
    }
}
