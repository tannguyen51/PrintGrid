using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLabQcProofApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QcProofPhotoKeys",
                schema: "scheduling",
                table: "jobs",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QcProofStatus",
                schema: "scheduling",
                table: "jobs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "QcRejectionReason",
                schema: "scheduling",
                table: "jobs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QcReviewedAtUtc",
                schema: "scheduling",
                table: "jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QcReviewedBy",
                schema: "scheduling",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QcSelfReport",
                schema: "scheduling",
                table: "jobs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QcProofPhotoKeys",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "QcProofStatus",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "QcRejectionReason",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "QcReviewedAtUtc",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "QcReviewedBy",
                schema: "scheduling",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "QcSelfReport",
                schema: "scheduling",
                table: "jobs");
        }
    }
}
