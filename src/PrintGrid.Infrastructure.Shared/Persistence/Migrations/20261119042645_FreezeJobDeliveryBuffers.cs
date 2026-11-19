using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FreezeJobDeliveryBuffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryBufferDays",
                schema: "scheduling",
                table: "jobs",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "LastMileDays",
                schema: "scheduling",
                table: "jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryBufferDays",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "LastMileDays",
                schema: "scheduling",
                table: "jobs");
        }
    }
}
