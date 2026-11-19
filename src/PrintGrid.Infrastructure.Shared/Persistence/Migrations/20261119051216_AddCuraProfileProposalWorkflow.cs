using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCuraProfileProposalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                schema: "scheduling",
                table: "cura_profiles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.AddColumn<string>(
                name: "ReviewReason",
                schema: "scheduling",
                table: "cura_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                schema: "scheduling",
                table: "cura_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                schema: "scheduling",
                table: "cura_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceFileName",
                schema: "scheduling",
                table: "cura_profiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionNotes",
                schema: "scheduling",
                table: "cura_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByLabId",
                schema: "scheduling",
                table: "cura_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                schema: "scheduling",
                table: "cura_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetMachineId",
                schema: "scheduling",
                table: "cura_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                schema: "scheduling",
                table: "cura_profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.CreateIndex(
                name: "IX_cura_profiles_ApprovalStatus_IsActive",
                schema: "scheduling",
                table: "cura_profiles",
                columns: new[] { "ApprovalStatus", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_cura_profiles_MaterialCode",
                schema: "scheduling",
                table: "cura_profiles",
                column: "MaterialCode");

            migrationBuilder.CreateIndex(
                name: "IX_cura_profiles_SubmittedByLabId_ApprovalStatus",
                schema: "scheduling",
                table: "cura_profiles",
                columns: new[] { "SubmittedByLabId", "ApprovalStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cura_profiles_ApprovalStatus_IsActive",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropIndex(
                name: "IX_cura_profiles_MaterialCode",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropIndex(
                name: "IX_cura_profiles_SubmittedByLabId_ApprovalStatus",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "ReviewReason",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "SourceFileName",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "SubmissionNotes",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "SubmittedByLabId",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "TargetMachineId",
                schema: "scheduling",
                table: "cura_profiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                schema: "scheduling",
                table: "cura_profiles");
        }
    }
}
