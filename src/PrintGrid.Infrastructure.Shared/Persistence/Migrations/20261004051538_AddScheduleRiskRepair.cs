using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations;

public partial class AddScheduleRiskRepair : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "ParentJobId", schema: "scheduling", table: "jobs", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<int>(name: "Quantity", schema: "scheduling", table: "jobs", type: "integer", nullable: false, defaultValue: 1);
        migrationBuilder.CreateTable(
            name: "date_change_requests", schema: "scheduling",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false), JobId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderItemId = table.Column<Guid>(type: "uuid", nullable: false), CurrentDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                ProposedDueDate = table.Column<DateOnly>(type: "date", nullable: false), Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_date_change_requests", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_jobs_ParentJobId", schema: "scheduling", table: "jobs", column: "ParentJobId");
        migrationBuilder.CreateIndex(name: "IX_date_change_requests_JobId_Status", schema: "scheduling", table: "date_change_requests", columns: new[] { "JobId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "date_change_requests", schema: "scheduling");
        migrationBuilder.DropIndex(name: "IX_jobs_ParentJobId", schema: "scheduling", table: "jobs");
        migrationBuilder.DropColumn(name: "ParentJobId", schema: "scheduling", table: "jobs");
        migrationBuilder.DropColumn(name: "Quantity", schema: "scheduling", table: "jobs");
    }
}
