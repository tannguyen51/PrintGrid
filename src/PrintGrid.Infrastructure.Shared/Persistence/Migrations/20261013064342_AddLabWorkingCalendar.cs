using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLabWorkingCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClosingTime",
                schema: "scheduling",
                table: "labs",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "17:00");

            migrationBuilder.AddColumn<string>(
                name: "OpeningTime",
                schema: "scheduling",
                table: "labs",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "08:00");

            migrationBuilder.AddColumn<string>(
                name: "WorkingDays",
                schema: "scheduling",
                table: "labs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Monday,Tuesday,Wednesday,Thursday,Friday");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosingTime",
                schema: "scheduling",
                table: "labs");

            migrationBuilder.DropColumn(
                name: "OpeningTime",
                schema: "scheduling",
                table: "labs");

            migrationBuilder.DropColumn(
                name: "WorkingDays",
                schema: "scheduling",
                table: "labs");
        }
    }
}
