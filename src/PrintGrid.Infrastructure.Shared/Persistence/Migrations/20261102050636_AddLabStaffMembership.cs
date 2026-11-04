using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLabStaffMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lab_staff",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_staff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lab_staff_labs_LabId",
                        column: x => x.LabId,
                        principalSchema: "scheduling",
                        principalTable: "labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lab_staff_LabId_UserId",
                schema: "scheduling",
                table: "lab_staff",
                columns: new[] { "LabId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lab_staff_UserId",
                schema: "scheduling",
                table: "lab_staff",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lab_staff",
                schema: "scheduling");
        }
    }
}
