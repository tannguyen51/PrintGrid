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
            // OwnerUserId and IX_labs_OwnerUserId are created by 20261012070000_AddWeek4LabAndHubConsolidation;
            // repeating them here made the merged migration chain fail on a fresh database
            // (42701: column "OwnerUserId" already exists). Only the capacity column remains.
            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentJobs",
                schema: "scheduling",
                table: "labs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxConcurrentJobs",
                schema: "scheduling",
                table: "labs");
        }
    }
}
