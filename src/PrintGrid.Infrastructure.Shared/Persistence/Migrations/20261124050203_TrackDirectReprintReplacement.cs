using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackDirectReprintReplacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReprintOfJobId",
                schema: "scheduling",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_ReprintOfJobId",
                schema: "scheduling",
                table: "jobs",
                column: "ReprintOfJobId",
                unique: true,
                filter: "\"ReprintOfJobId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_jobs_ReprintOfJobId",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "ReprintOfJobId",
                schema: "scheduling",
                table: "jobs");
        }
    }
}
