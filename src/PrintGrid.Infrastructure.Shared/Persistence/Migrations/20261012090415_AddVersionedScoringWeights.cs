using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedScoringWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scoring_weight_configurations",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DueDateSlack = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    LabReliability = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    LoadBalance = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    Transit = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scoring_weight_configurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scoring_weight_configurations_IsActive",
                schema: "scheduling",
                table: "scoring_weight_configurations",
                column: "IsActive",
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_scoring_weight_configurations_Version",
                schema: "scheduling",
                table: "scoring_weight_configurations",
                column: "Version",
                unique: true);

            migrationBuilder.InsertData(
                schema: "scheduling",
                table: "scoring_weight_configurations",
                columns: new[] { "Id", "Version", "DueDateSlack", "LabReliability", "LoadBalance", "Cost", "Transit", "IsActive", "Reason", "ChangedBy", "CreatedAtUtc" },
                values: new object[]
                {
                    new Guid("aa1ddfba-2c27-4f47-91c2-66d255841026"),
                    "2026.10",
                    0.35m, 0.25m, 0.20m, 0.10m, 0.10m,
                    true,
                    "Initial scoring configuration migrated from the assignment engine",
                    Guid.Empty,
                    new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc)
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scoring_weight_configurations",
                schema: "scheduling");
        }
    }
}
