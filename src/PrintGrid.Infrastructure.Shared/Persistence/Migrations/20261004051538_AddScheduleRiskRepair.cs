using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintGrid.Infrastructure.Shared.Persistence.Migrations;

/// <summary>
/// Split batches for the at-risk repair (BR-SCHED-010).
///
/// The branch that introduced this migration also created its own `scheduling.date_change_requests`
/// here. That table is owned by 20261006164643_RescheduleAndDecisionLog (the S8 "xin dời" entity the
/// customer approval link drives), so creating it here as well would make a fresh database fail with
/// "relation already exists". Only the split-related columns remain.
/// </summary>
public partial class AddScheduleRiskRepair : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "ParentJobId", schema: "scheduling", table: "jobs", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<int>(name: "Quantity", schema: "scheduling", table: "jobs", type: "integer", nullable: false, defaultValue: 1);
        migrationBuilder.CreateIndex(name: "IX_jobs_ParentJobId", schema: "scheduling", table: "jobs", column: "ParentJobId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_jobs_ParentJobId", schema: "scheduling", table: "jobs");
        migrationBuilder.DropColumn(name: "ParentJobId", schema: "scheduling", table: "jobs");
        migrationBuilder.DropColumn(name: "Quantity", schema: "scheduling", table: "jobs");
    }
}
