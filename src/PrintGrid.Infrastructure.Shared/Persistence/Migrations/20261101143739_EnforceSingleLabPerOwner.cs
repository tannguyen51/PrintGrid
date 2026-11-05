using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations;

/// <inheritdoc />
public partial class EnforceSingleLabPerOwner : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_labs_OwnerUserId",
            schema: "scheduling",
            table: "labs");

        migrationBuilder.CreateIndex(
            name: "IX_labs_OwnerUserId",
            schema: "scheduling",
            table: "labs",
            column: "OwnerUserId",
            unique: true,
            filter: "\"OwnerUserId\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_labs_OwnerUserId",
            schema: "scheduling",
            table: "labs");

        migrationBuilder.CreateIndex(
            name: "IX_labs_OwnerUserId",
            schema: "scheduling",
            table: "labs",
            column: "OwnerUserId");
    }
}
